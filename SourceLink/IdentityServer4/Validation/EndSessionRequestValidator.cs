// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Logging;
using IdentityServer4.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System;
using IdentityServer4.Models;
using static IdentityServer4.IdentityServerConstants;

namespace IdentityServer4.Validation
{
    internal class EndSessionRequestValidator : IEndSessionRequestValidator
    {
        private readonly ILogger _logger;
        private readonly IdentityServerOptions _options;
        private readonly ITokenValidator _tokenValidator;
        private readonly IRedirectUriValidator _uriValidator;
        private readonly IUserSession _userSession;
        private readonly IClientStore _clientStore;
        private readonly IMessageStore<EndSession> _endSessionMessageStore;
        private readonly IHttpContextAccessor _context;

        public EndSessionRequestValidator(
            IHttpContextAccessor context,
            IdentityServerOptions options,
            ITokenValidator tokenValidator,
            IRedirectUriValidator uriValidator,
            IUserSession userSession,
            IClientStore clientStore,
            IMessageStore<EndSession> endSessionMessageStore,
            ILogger<EndSessionRequestValidator> logger)
        {
            _context = context;
            _options = options;
            _tokenValidator = tokenValidator;
            _uriValidator = uriValidator;
            _userSession = userSession;
            _clientStore = clientStore;
            _endSessionMessageStore = endSessionMessageStore;
            _logger = logger;
        }

        public async Task<EndSessionValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal subject)
        {
            _logger.LogDebug("Start end session request validation");

            var isAuthenticated = subject.IsAuthenticated();

            if (!isAuthenticated && _options.Authentication.RequireAuthenticatedUserForSignOutMessage)
            {
                return Invalid("User is anonymous. Ignoring end session parameters");
            }

            var validatedRequest = new ValidatedEndSessionRequest
            {
                Raw = parameters
            };

            var idTokenHint = parameters.Get(OidcConstants.EndSessionRequest.IdTokenHint);
            if (idTokenHint.IsPresent())
            {
                // validate id_token - no need to validate token life time
                var tokenValidationResult = await _tokenValidator.ValidateIdentityTokenAsync(idTokenHint, null, false);
                if (tokenValidationResult.IsError)
                {
                    return Invalid("Error validating id token hint", validatedRequest);
                }

                validatedRequest.Client = tokenValidationResult.Client;

                // validate sub claim against currently logged on user
                var subClaim = tokenValidationResult.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Subject);
                if (subClaim != null && isAuthenticated)
                {
                    if (subject.GetSubjectId() != subClaim.Value)
                    {
                        return Invalid("Current user does not match identity token", validatedRequest);
                    }

                    validatedRequest.Subject = subject;
                    validatedRequest.SessionId = await _userSession.GetSessionIdAsync();
                    validatedRequest.ClientIds = await _userSession.GetClientListAsync();
                }

                var redirectUri = parameters.Get(OidcConstants.EndSessionRequest.PostLogoutRedirectUri);
                if (redirectUri.IsPresent())
                {
                    if (await _uriValidator.IsPostLogoutRedirectUriValidAsync(redirectUri, validatedRequest.Client) == false)
                    {
                        return Invalid("Invalid post logout URI", validatedRequest);
                    }

                    validatedRequest.PostLogOutUri = redirectUri;
                }
                else if (validatedRequest.Client.PostLogoutRedirectUris.Count == 1)
                {
                    validatedRequest.PostLogOutUri = validatedRequest.Client.PostLogoutRedirectUris.First();
                }

                if (validatedRequest.PostLogOutUri != null)
                {
                    var state = parameters.Get(OidcConstants.EndSessionRequest.State);
                    if (state.IsPresent())
                    {
                        validatedRequest.State = state;
                    }
                }
            }
            else
            {
                // no id_token to authenticate the client, but we do have a user and a user session
                validatedRequest.Subject = subject;
                validatedRequest.SessionId = await _userSession.GetSessionIdAsync();
                validatedRequest.ClientIds = await _userSession.GetClientListAsync();
            }

            LogSuccess(validatedRequest);

            return new EndSessionValidationResult
            {
                ValidatedRequest = validatedRequest,
                IsError = false
            };
        }

        private EndSessionValidationResult Invalid(string message, ValidatedEndSessionRequest request = null)
        {
            message = "End session request validation failure: " + message;
            if (request != null)
            {
                var log = new EndSessionRequestValidationLog(request);
                _logger.LogInformation(message + Environment.NewLine + "{details}", log);
            }
            else
            {
                _logger.LogInformation(message);
            }

            return new EndSessionValidationResult
            {
                IsError = true,
                Error = "Invalid request",
                ErrorDescription = message
            };
        }

        private void LogSuccess(ValidatedEndSessionRequest request)
        {
            var log = new EndSessionRequestValidationLog(request);
            _logger.LogInformation("End session request validation success" + Environment.NewLine + "{details}", log);
        }

        public async Task<EndSessionCallbackValidationResult> ValidateCallbackAsync(NameValueCollection parameters)
        {
            var result = new EndSessionCallbackValidationResult
            {
                IsError = true
            };

            var endSessionId = parameters[Constants.UIConstants.DefaultRoutePathParams.EndSessionCallback];
            var endSessionMessage = await _endSessionMessageStore.ReadAsync(endSessionId);
            if (endSessionMessage?.Data?.ClientIds?.Any() == true)
            {
                result.IsError = false;

                var (frontChannel, backChannel) = await GetClientEndSessionUrlsAsync(endSessionMessage.Data);
                result.FrontChannelLogoutUrls = frontChannel;
                result.BackChannelLogouts = backChannel;
            }

            return result;
        }

        private async Task<(IEnumerable<string> frontChannel, IEnumerable<BackChannelLogoutModel> backChannel)> GetClientEndSessionUrlsAsync(EndSession endSession)
        {
            var frontChannelUrls = new List<string>();
            var backChannelLogouts = new List<BackChannelLogoutModel>();
            foreach (var clientId in endSession.ClientIds)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(clientId);
                if (client != null)
                {
                    if (client.FrontChannelLogoutUri.IsPresent())
                    {
                        var url = client.FrontChannelLogoutUri;

                        // add session id if required
                        if (client.ProtocolType == ProtocolTypes.OpenIdConnect)
                        {
                            if (client.FrontChannelLogoutSessionRequired)
                            {
                                url = url.AddQueryString(OidcConstants.EndSessionRequest.Sid, endSession.SessionId);
                                url = url.AddQueryString(OidcConstants.EndSessionRequest.Issuer, _context.HttpContext.GetIdentityServerIssuerUri());
                            }
                        }
                        else if (client.ProtocolType == ProtocolTypes.WsFederation)
                        {
                            url = url.AddQueryString(Constants.WsFedSignOut.LogoutUriParameterName, Constants.WsFedSignOut.LogoutUriParameterValue);
                        }

                        frontChannelUrls.Add(url);
                    }

                    if (client.BackChannelLogoutUri.IsPresent())
                    {
                        var back = new BackChannelLogoutModel
                        {
                            ClientId = clientId,
                            LogoutUri = client.BackChannelLogoutUri,
                            SubjectId = endSession.SubjectId,
                            SessionId = endSession.SessionId,
                            SessionIdRequired = client.BackChannelLogoutSessionRequired
                        };

                        backChannelLogouts.Add(back);
                    }
                }
            }

            if (frontChannelUrls.Any())
            {
                var msg = frontChannelUrls.Aggregate((x, y) => x + ", " + y);
                _logger.LogDebug("Client front-channel logout URLs: {0}", msg);
            }
            else
            {
                _logger.LogDebug("No client front-channel logout URLs");
            }

            if (backChannelLogouts.Any())
            {
                var msg = backChannelLogouts.Select(x => x.LogoutUri).Aggregate((x, y) => x + ", " + y);
                _logger.LogDebug("Client back-channel logout URLs: {0}", msg);
            }
            else
            {
                _logger.LogDebug("No client back-channel logout URLs");
            }

            return (frontChannelUrls, backChannelLogouts);
        }
    }
}