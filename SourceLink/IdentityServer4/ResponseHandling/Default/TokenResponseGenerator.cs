// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace IdentityServer4.ResponseHandling
{
    /// <summary>
    /// The default token response generator
    /// </summary>
    /// <seealso cref="IdentityServer4.ResponseHandling.ITokenResponseGenerator" />
    public class TokenResponseGenerator : ITokenResponseGenerator
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The token service
        /// </summary>
        protected readonly ITokenService TokenService;

        /// <summary>
        /// The refresh token service
        /// </summary>
        protected readonly IRefreshTokenService RefreshTokenService;

        /// <summary>
        /// The resource store
        /// </summary>
        protected readonly IResourceStore Resources;

        /// <summary>
        /// The clients store
        /// </summary>
        protected readonly IClientStore Clients;

        /// <summary>
        ///  The clock
        /// </summary>
        protected readonly ISystemClock Clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenResponseGenerator" /> class.
        /// </summary>
        /// <param name="clock">The clock.</param>
        /// <param name="tokenService">The token service.</param>
        /// <param name="refreshTokenService">The refresh token service.</param>
        /// <param name="resources">The resources.</param>
        /// <param name="clients">The clients.</param>
        /// <param name="logger">The logger.</param>
        public TokenResponseGenerator(ISystemClock clock, ITokenService tokenService, IRefreshTokenService refreshTokenService, IResourceStore resources, IClientStore clients, ILogger<TokenResponseGenerator> logger)
        {
            Clock = clock;
            TokenService = tokenService;
            RefreshTokenService = refreshTokenService;
            Resources = resources;
            Clients = clients;
            Logger = logger;
        }

        /// <summary>
        /// Processes the response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public virtual async Task<TokenResponse> ProcessAsync(TokenRequestValidationResult request)
        {
            switch (request.ValidatedRequest.GrantType)
            {
                case OidcConstants.GrantTypes.ClientCredentials:
                    return await ProcessClientCredentialsRequestAsync(request);
                case OidcConstants.GrantTypes.Password:
                    return await ProcessPasswordRequestAsync(request);
                case OidcConstants.GrantTypes.AuthorizationCode:
                    return await ProcessAuthorizationCodeRequestAsync(request);
                case OidcConstants.GrantTypes.RefreshToken:
                    return await ProcessRefreshTokenRequestAsync(request);
                case OidcConstants.GrantTypes.DeviceCode:
                    return await ProcessDeviceCodeRequestAsync(request);
                default:
                    return await ProcessExtensionGrantRequestAsync(request);
            }
        }

        /// <summary>
        /// Creates the response for an client credentials request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        protected virtual Task<TokenResponse> ProcessClientCredentialsRequestAsync(TokenRequestValidationResult request)
        {
            Logger.LogTrace("Creating response for client credentials request");

            return ProcessTokenRequestAsync(request);
        }

        /// <summary>
        /// Creates the response for a password request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        protected virtual Task<TokenResponse> ProcessPasswordRequestAsync(TokenRequestValidationResult request)
        {
            Logger.LogTrace("Creating response for password request");

            return ProcessTokenRequestAsync(request);
        }

        /// <summary>
        /// Creates the response for an authorization code request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Client does not exist anymore.</exception>
        protected virtual async Task<TokenResponse> ProcessAuthorizationCodeRequestAsync(TokenRequestValidationResult request)
        {
            Logger.LogTrace("Creating response for authorization code request");

            //////////////////////////
            // access token
            /////////////////////////
            (var accessToken, var refreshToken) = await CreateAccessTokenAsync(request.ValidatedRequest);
            var response = new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenLifetime = request.ValidatedRequest.AccessTokenLifetime,
                Custom = request.CustomResponse
            };

            //////////////////////////
            // refresh token
            /////////////////////////
            if (refreshToken.IsPresent())
            {
                response.RefreshToken = refreshToken;
            }

            //////////////////////////
            // id token
            /////////////////////////
            if (request.ValidatedRequest.AuthorizationCode.IsOpenId)
            {
                // load the client that belongs to the authorization code
                Client client = null;
                if (request.ValidatedRequest.AuthorizationCode.ClientId != null)
                {
                    client = await Clients.FindEnabledClientByIdAsync(request.ValidatedRequest.AuthorizationCode.ClientId);
                }
                if (client == null)
                {
                    throw new InvalidOperationException("Client does not exist anymore.");
                }

                var resources = await Resources.FindEnabledResourcesByScopeAsync(request.ValidatedRequest.AuthorizationCode.RequestedScopes);

                var tokenRequest = new TokenCreationRequest
                {
                    Subject = request.ValidatedRequest.AuthorizationCode.Subject,
                    Resources = resources,
                    Nonce = request.ValidatedRequest.AuthorizationCode.Nonce,
                    AccessTokenToHash = response.AccessToken,
                    ValidatedRequest = request.ValidatedRequest
                };

                var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);
                var jwt = await TokenService.CreateSecurityTokenAsync(idToken);
                response.IdentityToken = jwt;
            }

            return response;
        }

        /// <summary>
        /// Creates the response for a refresh token request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        protected virtual async Task<TokenResponse> ProcessRefreshTokenRequestAsync(TokenRequestValidationResult request)
        {
            Logger.LogTrace("Creating response for refresh token request");

            var oldAccessToken = request.ValidatedRequest.RefreshToken.AccessToken;
            string accessTokenString;

            if (request.ValidatedRequest.Client.UpdateAccessTokenClaimsOnRefresh)
            {
                var subject = request.ValidatedRequest.RefreshToken.Subject;

                var creationRequest = new TokenCreationRequest
                {
                    Subject = subject,
                    ValidatedRequest = request.ValidatedRequest,
                    Resources = await Resources.FindEnabledResourcesByScopeAsync(oldAccessToken.Scopes)
                };

                var newAccessToken = await TokenService.CreateAccessTokenAsync(creationRequest);
                accessTokenString = await TokenService.CreateSecurityTokenAsync(newAccessToken);
            }
            else
            {
                oldAccessToken.CreationTime = Clock.UtcNow.UtcDateTime;
                oldAccessToken.Lifetime = request.ValidatedRequest.AccessTokenLifetime;

                accessTokenString = await TokenService.CreateSecurityTokenAsync(oldAccessToken);
            }

            var handle = await RefreshTokenService.UpdateRefreshTokenAsync(request.ValidatedRequest.RefreshTokenHandle, request.ValidatedRequest.RefreshToken, request.ValidatedRequest.Client);

            return new TokenResponse
            {
                IdentityToken = await CreateIdTokenFromRefreshTokenRequestAsync(request.ValidatedRequest, accessTokenString),
                AccessToken = accessTokenString,
                AccessTokenLifetime = request.ValidatedRequest.AccessTokenLifetime,
                RefreshToken = handle,
                Custom = request.CustomResponse
            };
        }

        /// <summary>
        /// Processes the response for device code grant request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        protected virtual async Task<TokenResponse> ProcessDeviceCodeRequestAsync(TokenRequestValidationResult request)
        {
            Logger.LogTrace("Creating response for device code request");

            //////////////////////////
            // access token
            /////////////////////////
            var (accessToken, refreshToken) = await CreateAccessTokenAsync(request.ValidatedRequest);
            var response = new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenLifetime = request.ValidatedRequest.AccessTokenLifetime,
                Custom = request.CustomResponse
            };

            //////////////////////////
            // refresh token
            /////////////////////////
            if (refreshToken.IsPresent())
            {
                response.RefreshToken = refreshToken;
            }

            //////////////////////////
            // id token
            /////////////////////////
            if (request.ValidatedRequest.DeviceCode.IsOpenId)
            {
                // load the client that belongs to the device code
                Client client = null;
                if (request.ValidatedRequest.DeviceCode.ClientId != null)
                {
                    client = await Clients.FindEnabledClientByIdAsync(request.ValidatedRequest.DeviceCode.ClientId);
                }
                if (client == null)
                {
                    throw new InvalidOperationException("Client does not exist anymore.");
                }

                var resources = await Resources.FindEnabledResourcesByScopeAsync(request.ValidatedRequest.DeviceCode.AuthorizedScopes);

                var tokenRequest = new TokenCreationRequest
                {
                    Subject = request.ValidatedRequest.DeviceCode.Subject,
                    Resources = resources,
                    AccessTokenToHash = response.AccessToken,
                    ValidatedRequest = request.ValidatedRequest
                };

                var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);
                var jwt = await TokenService.CreateSecurityTokenAsync(idToken);
                response.IdentityToken = jwt;
            }

            return response;
        }

        /// <summary>
        /// Creates the response for an extension grant request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        protected virtual Task<TokenResponse> ProcessExtensionGrantRequestAsync(TokenRequestValidationResult request)
        {
            Logger.LogTrace("Creating response for extension grant request");

            return ProcessTokenRequestAsync(request);
        }

        /// <summary>
        /// Creates the response for a token request.
        /// </summary>
        /// <param name="validationResult">The validation result.</param>
        /// <returns></returns>
        protected virtual async Task<TokenResponse> ProcessTokenRequestAsync(TokenRequestValidationResult validationResult)
        {
            (var accessToken, var refreshToken) = await CreateAccessTokenAsync(validationResult.ValidatedRequest);
            var response = new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenLifetime = validationResult.ValidatedRequest.AccessTokenLifetime,
                Custom = validationResult.CustomResponse
            };

            if (refreshToken.IsPresent())
            {
                response.RefreshToken = refreshToken;
            }

            return response;
        }

        /// <summary>
        /// Creates the access/refresh token.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Client does not exist anymore.</exception>
        protected virtual async Task<(string accessToken, string refreshToken)> CreateAccessTokenAsync(ValidatedTokenRequest request)
        {
            TokenCreationRequest tokenRequest;
            bool createRefreshToken;

            if (request.AuthorizationCode != null)
            {
                createRefreshToken = request.AuthorizationCode.RequestedScopes.Contains(IdentityServerConstants.StandardScopes.OfflineAccess);

                // load the client that belongs to the authorization code
                Client client = null;
                if (request.AuthorizationCode.ClientId != null)
                {
                    client = await Clients.FindEnabledClientByIdAsync(request.AuthorizationCode.ClientId);
                }
                if (client == null)
                {
                    throw new InvalidOperationException("Client does not exist anymore.");
                }

                var resources = await Resources.FindEnabledResourcesByScopeAsync(request.AuthorizationCode.RequestedScopes);

                tokenRequest = new TokenCreationRequest
                {
                    Subject = request.AuthorizationCode.Subject,
                    Resources = resources,
                    ValidatedRequest = request
                };
            }
            else if (request.DeviceCode != null)
            {
                createRefreshToken = request.DeviceCode.AuthorizedScopes.Contains(IdentityServerConstants.StandardScopes.OfflineAccess);

                Client client = null;
                if (request.DeviceCode.ClientId != null)
                {
                    client = await Clients.FindEnabledClientByIdAsync(request.DeviceCode.ClientId);
                }
                if (client == null)
                {
                    throw new InvalidOperationException("Client does not exist anymore.");
                }

                var resources = await Resources.FindEnabledResourcesByScopeAsync(request.DeviceCode.AuthorizedScopes);

                tokenRequest = new TokenCreationRequest
                {
                    Subject = request.DeviceCode.Subject,
                    Resources = resources,
                    ValidatedRequest = request
                };
            }
            else
            {
                createRefreshToken = request.ValidatedScopes.ContainsOfflineAccessScope;

                tokenRequest = new TokenCreationRequest
                {
                    Subject = request.Subject,
                    Resources = request.ValidatedScopes.GrantedResources,
                    ValidatedRequest = request
                };
            }

            var at = await TokenService.CreateAccessTokenAsync(tokenRequest);
            var accessToken = await TokenService.CreateSecurityTokenAsync(at);

            if (createRefreshToken)
            {
                var refreshToken = await RefreshTokenService.CreateRefreshTokenAsync(tokenRequest.Subject, at, request.Client);
                return (accessToken, refreshToken);
            }

            return (accessToken, null);
        }

        /// <summary>
        /// Creates an id_token for a refresh token request if identity resources have been requested.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="newAccessToken">The new access token.</param>
        /// <returns></returns>
        protected virtual async Task<string> CreateIdTokenFromRefreshTokenRequestAsync(ValidatedTokenRequest request, string newAccessToken)
        {
            var resources = await Resources.FindResourcesByScopeAsync(request.RefreshToken.Scopes);
            if (resources.IdentityResources.Any())
            {
                var oldAccessToken = request.RefreshToken.AccessToken;
                var tokenRequest = new TokenCreationRequest
                {
                    Subject = request.RefreshToken.Subject,
                    Resources = await Resources.FindEnabledResourcesByScopeAsync(oldAccessToken.Scopes),
                    ValidatedRequest = request,
                    AccessTokenToHash = newAccessToken
                };

                var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);
                return await TokenService.CreateSecurityTokenAsync(idToken);
            }

            return null;
        }
    }
}