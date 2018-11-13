// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using IdentityServer4.Stores;
using IdentityServer4.Configuration;
using IdentityServer4.Logging.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace IdentityServer4.Validation
{
    internal class TokenValidator : ITokenValidator
    {
        private readonly ILogger _logger;
        private readonly IdentityServerOptions _options;
        private readonly IHttpContextAccessor _context;
        private readonly IReferenceTokenStore _referenceTokenStore;
        private readonly IRefreshTokenStore _refreshTokenStore;
        private readonly ICustomTokenValidator _customValidator;
        private readonly IClientStore _clients;
        private readonly IProfileService _profile;
        private readonly IKeyMaterialService _keys;
        private readonly ISystemClock _clock;
        private readonly TokenValidationLog _log;

        public TokenValidator(
            IdentityServerOptions options,
            IHttpContextAccessor context,
            IClientStore clients,
            IProfileService profile,
            IReferenceTokenStore referenceTokenStore,
            IRefreshTokenStore refreshTokenStore,
            ICustomTokenValidator customValidator,
            IKeyMaterialService keys,
            ISystemClock clock,
            ILogger<TokenValidator> logger)
        {
            _options = options;
            _context = context;
            _clients = clients;
            _profile = profile;
            _referenceTokenStore = referenceTokenStore;
            _refreshTokenStore = refreshTokenStore;
            _customValidator = customValidator;
            _keys = keys;
            _clock = clock;
            _logger = logger;

            _log = new TokenValidationLog();
        }

        public async Task<TokenValidationResult> ValidateIdentityTokenAsync(string token, string clientId = null, bool validateLifetime = true)
        {
            _logger.LogDebug("Start identity token validation");

            if (token.Length > _options.InputLengthRestrictions.Jwt)
            {
                _logger.LogError("JWT too long");
                return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
            }

            if (clientId.IsMissing())
            {
                clientId = GetClientIdFromJwt(token);

                if (clientId.IsMissing())
                {
                    _logger.LogError("No clientId supplied, can't find id in identity token.");
                    return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
                }
            }

            _log.ClientId = clientId;
            _log.ValidateLifetime = validateLifetime;

            var client = await _clients.FindEnabledClientByIdAsync(clientId);
            if (client == null)
            {
                _logger.LogError("Unknown or disabled client: {clientId}.", clientId);
                return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
            }

            _log.ClientName = client.ClientName;
            _logger.LogDebug("Client found: {clientId} / {clientName}", client.ClientId, client.ClientName);

            var keys = await _keys.GetValidationKeysAsync();
            var result = await ValidateJwtAsync(token, clientId, keys, validateLifetime);

            result.Client = client;

            if (result.IsError)
            {
                LogError("Error validating JWT");
                return result;
            }

            _log.Claims = result.Claims.ToClaimsDictionary();

            // make sure user is still active (if sub claim is present)
            var subClaim = result.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Subject);
            if (subClaim != null)
            {
                var principal = Principal.Create("tokenvalidator", result.Claims.ToArray());

                var isActiveCtx = new IsActiveContext(principal, result.Client, IdentityServerConstants.ProfileIsActiveCallers.IdentityTokenValidation);
                await _profile.IsActiveAsync(isActiveCtx);

                if (isActiveCtx.IsActive == false)
                {
                    _logger.LogError("User marked as not active: {subject}", subClaim.Value);

                    result.IsError = true;
                    result.Error = OidcConstants.ProtectedResourceErrors.InvalidToken;
                    result.Claims = null;

                    return result;
                }
            }

            _logger.LogDebug("Calling into custom token validator: {type}", _customValidator.GetType().FullName);
            var customResult = await _customValidator.ValidateIdentityTokenAsync(result);

            if (customResult.IsError)
            {
                LogError("Custom validator failed: " + (customResult.Error ?? "unknown"));
                return customResult;
            }

            _log.Claims = customResult.Claims.ToClaimsDictionary();

            LogSuccess();
            return customResult;
        }

        public async Task<TokenValidationResult> ValidateAccessTokenAsync(string token, string expectedScope = null)
        {
            _logger.LogTrace("Start access token validation");

            _log.ExpectedScope = expectedScope;
            _log.ValidateLifetime = true;

            TokenValidationResult result;

            if (token.Contains("."))
            {
                if (token.Length > _options.InputLengthRestrictions.Jwt)
                {
                    _logger.LogError("JWT too long");

                    return new TokenValidationResult
                    {
                        IsError = true,
                        Error = OidcConstants.ProtectedResourceErrors.InvalidToken,
                        ErrorDescription = "Token too long"
                    };
                }

                _log.AccessTokenType = AccessTokenType.Jwt.ToString();
                result = await ValidateJwtAsync(
                    token,
                    string.Format(IdentityServerConstants.AccessTokenAudience, _context.HttpContext.GetIdentityServerIssuerUri().EnsureTrailingSlash()),
                    await _keys.GetValidationKeysAsync());
            }
            else
            {
                if (token.Length > _options.InputLengthRestrictions.TokenHandle)
                {
                    _logger.LogError("token handle too long");

                    return new TokenValidationResult
                    {
                        IsError = true,
                        Error = OidcConstants.ProtectedResourceErrors.InvalidToken,
                        ErrorDescription = "Token too long"
                    };
                }

                _log.AccessTokenType = AccessTokenType.Reference.ToString();
                result = await ValidateReferenceAccessTokenAsync(token);
            }

            _log.Claims = result.Claims.ToClaimsDictionary();

            if (result.IsError)
            {
                return result;
            }

            // make sure client is still active (if client_id claim is present)
            var clientClaim = result.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.ClientId);
            if (clientClaim != null)
            {
                var client = await _clients.FindEnabledClientByIdAsync(clientClaim.Value);
                if (client == null)
                {
                    _logger.LogError("Client deleted or disabled: {clientId}", clientClaim.Value);

                    result.IsError = true;
                    result.Error = OidcConstants.ProtectedResourceErrors.InvalidToken;
                    result.Claims = null;

                    return result;
                }
            }

            // make sure user is still active (if sub claim is present)
            var subClaim = result.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Subject);
            if (subClaim != null)
            {
                var principal = Principal.Create("tokenvalidator", result.Claims.ToArray());

                if (result.ReferenceTokenId.IsPresent())
                {
                    principal.Identities.First().AddClaim(new Claim(JwtClaimTypes.ReferenceTokenId, result.ReferenceTokenId));
                }

                var isActiveCtx = new IsActiveContext(principal, result.Client, IdentityServerConstants.ProfileIsActiveCallers.AccessTokenValidation);
                await _profile.IsActiveAsync(isActiveCtx);

                if (isActiveCtx.IsActive == false)
                {
                    _logger.LogError("User marked as not active: {subject}", subClaim.Value);

                    result.IsError = true;
                    result.Error = OidcConstants.ProtectedResourceErrors.InvalidToken;
                    result.Claims = null;

                    return result;
                }
            }

            // check expected scope(s)
            if (expectedScope.IsPresent())
            {
                var scope = result.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Scope && c.Value == expectedScope);
                if (scope == null)
                {
                    LogError(string.Format("Checking for expected scope {0} failed", expectedScope));
                    return Invalid(OidcConstants.ProtectedResourceErrors.InsufficientScope);
                }
            }

            _logger.LogDebug("Calling into custom token validator: {type}", _customValidator.GetType().FullName);
            var customResult = await _customValidator.ValidateAccessTokenAsync(result);

            if (customResult.IsError)
            {
                LogError("Custom validator failed: " + (customResult.Error ?? "unknown"));
                return customResult;
            }

            // add claims again after custom validation
            _log.Claims = customResult.Claims.ToClaimsDictionary();

            LogSuccess();
            return customResult;
        }

        private async Task<TokenValidationResult> ValidateJwtAsync(string jwt, string audience, IEnumerable<SecurityKey> validationKeys, bool validateLifetime = true)
        {
            var handler = new JwtSecurityTokenHandler();
            handler.InboundClaimTypeMap.Clear();

            var parameters = new TokenValidationParameters
            {
                ValidIssuer = _context.HttpContext.GetIdentityServerIssuerUri(),
                IssuerSigningKeys = validationKeys,
                ValidateLifetime = validateLifetime,
                ValidAudience = audience
            };

            try
            {
                var id = handler.ValidateToken(jwt, parameters, out var _);

                // if access token contains an ID, log it
                var jwtId = id.FindFirst(JwtClaimTypes.JwtId);
                if (jwtId != null)
                {
                    _log.JwtId = jwtId.Value;
                }

                // load the client that belongs to the client_id claim
                Client client = null;
                var clientId = id.FindFirst(JwtClaimTypes.ClientId);
                if (clientId != null)
                {
                    client = await _clients.FindEnabledClientByIdAsync(clientId.Value);
                    if (client == null)
                    {
                        throw new InvalidOperationException("Client does not exist anymore.");
                    }
                }

                return new TokenValidationResult
                {
                    IsError = false,

                    Claims = id.Claims,
                    Client = client,
                    Jwt = jwt
                };
            }
            catch (SecurityTokenExpiredException expiredException)
            {
                _logger.LogInformation(expiredException, "JWT token validation error: {exception}", expiredException.Message);
                return Invalid(OidcConstants.ProtectedResourceErrors.ExpiredToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT token validation error: {exception}", ex.Message);
                return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
            }
        }

        private async Task<TokenValidationResult> ValidateReferenceAccessTokenAsync(string tokenHandle)
        {
            _log.TokenHandle = tokenHandle;
            var token = await _referenceTokenStore.GetReferenceTokenAsync(tokenHandle);

            if (token == null)
            {
                LogError("Invalid reference token.");
                return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
            }

            if (token.CreationTime.HasExceeded(token.Lifetime, _clock.UtcNow.UtcDateTime))
            {
                LogError("Token expired.");

                await _referenceTokenStore.RemoveReferenceTokenAsync(tokenHandle);
                return Invalid(OidcConstants.ProtectedResourceErrors.ExpiredToken);
            }

            // load the client that is defined in the token
            Client client = null;
            if (token.ClientId != null)
            {
                client = await _clients.FindEnabledClientByIdAsync(token.ClientId);
            }

            if (client == null)
            {
                LogError($"Client deleted or disabled: {token.ClientId}");
                return Invalid(OidcConstants.ProtectedResourceErrors.InvalidToken);
            }

            return new TokenValidationResult
            {
                IsError = false,

                Client = client,
                Claims = ReferenceTokenToClaims(token),
                ReferenceToken = token,
                ReferenceTokenId = tokenHandle
            };
        }

        public async Task<TokenValidationResult> ValidateRefreshTokenAsync(string tokenHandle, Client client = null)
        {
            _logger.LogTrace("Start refresh token validation");

            /////////////////////////////////////////////
            // check if refresh token is valid
            /////////////////////////////////////////////
            var refreshToken = await _refreshTokenStore.GetRefreshTokenAsync(tokenHandle);
            if (refreshToken == null)
            {
                _logger.LogWarning("Invalid refresh token");
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            /////////////////////////////////////////////
            // check if refresh token has expired
            /////////////////////////////////////////////
            if (refreshToken.CreationTime.HasExceeded(refreshToken.Lifetime, _clock.UtcNow.DateTime))
            {
                _logger.LogWarning("Refresh token has expired. Removing from store.");

                await _refreshTokenStore.RemoveRefreshTokenAsync(tokenHandle);
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            if (client != null)
            {
                /////////////////////////////////////////////
                // check if client belongs to requested refresh token
                /////////////////////////////////////////////
                if (client.ClientId != refreshToken.ClientId)
                {
                    _logger.LogError("{0} tries to refresh token belonging to {1}", client.ClientId, refreshToken.ClientId);
                    return Invalid(OidcConstants.TokenErrors.InvalidGrant);
                }

                /////////////////////////////////////////////
                // check if client still has offline_access scope
                /////////////////////////////////////////////
                if (!client.AllowOfflineAccess)
                {
                    _logger.LogError("{clientId} does not have access to offline_access scope anymore", client.ClientId);
                    return Invalid(OidcConstants.TokenErrors.InvalidGrant);
                }

                _log.ClientId = client.ClientId;
                _log.ClientName = client.ClientName;
            }

            /////////////////////////////////////////////
            // make sure user is enabled
            /////////////////////////////////////////////
            var isActiveCtx = new IsActiveContext(
                refreshToken.Subject,
                client,
                IdentityServerConstants.ProfileIsActiveCallers.RefreshTokenValidation);
            await _profile.IsActiveAsync(isActiveCtx);

            if (isActiveCtx.IsActive == false)
            {
                _logger.LogError("{subjectId} has been disabled", refreshToken.Subject.GetSubjectId());
                return Invalid(OidcConstants.TokenErrors.InvalidGrant);
            }

            _log.Claims = refreshToken.Subject.Claims.ToClaimsDictionary();

            LogSuccess();

            return new TokenValidationResult
            {
                IsError = false,
                RefreshToken = refreshToken
            };
        }

        private IEnumerable<Claim> ReferenceTokenToClaims(Token token)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Issuer, token.Issuer),
                new Claim(JwtClaimTypes.NotBefore, token.CreationTime.ToEpochTime().ToString(), ClaimValueTypes.Integer),
                new Claim(JwtClaimTypes.Expiration, token.CreationTime.AddSeconds(token.Lifetime).ToEpochTime().ToString(), ClaimValueTypes.Integer)
            };

            foreach (var aud in token.Audiences)
            {
                claims.Add(new Claim(JwtClaimTypes.Audience, aud));
            }

            claims.AddRange(token.Claims);
            return claims;
        }

        private string GetClientIdFromJwt(string token)
        {
            try
            {
                var jwt = new JwtSecurityToken(token);
                var clientId = jwt.Audiences.FirstOrDefault();

                return clientId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Malformed JWT token: {exception}", ex.Message);
                return null;
            }
        }

        private TokenValidationResult Invalid(string error)
        {
            return new TokenValidationResult
            {
                IsError = true,
                Error = error
            };
        }

        private void LogError(string message)
        {
            _logger.LogError(message + "\n{@logMessage}", _log);
        }

        private void LogSuccess()
        {
            _logger.LogDebug("Token validation success\n{@logMessage}", _log);
        }
    }
}
