// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
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
using IdentityServer4.Logging.Models;
using IdentityServer4.Models;
using static IdentityServer4.IdentityServerConstants;

namespace IdentityServer4.Validation
{
    /// <summary>
    /// Validates requests to the end session endpoint.
    /// </summary>
    public class EndSessionRequestValidator : IEndSessionRequestValidator
    {
        /// <summary>
        /// The logger.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        ///  The IdentityServer options.
        /// </summary>
        protected readonly IdentityServerOptions Options;

        /// <summary>
        /// The token validator.
        /// </summary>
        protected readonly ITokenValidator TokenValidator;

        /// <summary>
        /// The URI validator.
        /// </summary>
        protected readonly IRedirectUriValidator UriValidator;

        /// <summary>
        /// The user session service.
        /// </summary>
        protected readonly IUserSession UserSession;

        /// <summary>
        /// The client store.
        /// </summary>
        protected readonly IClientStore ClientStore;

        /// <summary>
        /// The end session message store.
        /// </summary>
        protected readonly IMessageStore<EndSession> EndSessionMessageStore;

        /// <summary>
        /// The HTTP context accessor.
        /// </summary>
        protected readonly IHttpContextAccessor Context;

        /// <summary>
        /// Creates a new instance of the EndSessionRequestValidator.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="tokenValidator"></param>
        /// <param name="uriValidator"></param>
        /// <param name="userSession"></param>
        /// <param name="clientStore"></param>
        /// <param name="endSessionMessageStore"></param>
        /// <param name="logger"></param>
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
            Context = context;
            Options = options;
            TokenValidator = tokenValidator;
            UriValidator = uriValidator;
            UserSession = userSession;
            ClientStore = clientStore;
            EndSessionMessageStore = endSessionMessageStore;
            Logger = logger;
        }

        /// <inheritdoc />
        public async Task<EndSessionValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal subject)
        {
            Logger.LogDebug("Start end session request validation");

            var isAuthenticated = subject.IsAuthenticated();

            if (!isAuthenticated && Options.Authentication.RequireAuthenticatedUserForSignOutMessage)
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
                var tokenValidationResult = await TokenValidator.ValidateIdentityTokenAsync(idTokenHint, null, false);
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
                    validatedRequest.SessionId = await UserSession.GetSessionIdAsync();
                    validatedRequest.ClientIds = await UserSession.GetClientListAsync();
                }

                var redirectUri = parameters.Get(OidcConstants.EndSessionRequest.PostLogoutRedirectUri);
                if (redirectUri.IsPresent())
                {
                    if (await UriValidator.IsPostLogoutRedirectUriValidAsync(redirectUri, validatedRequest.Client) == false)
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
                validatedRequest.SessionId = await UserSession.GetSessionIdAsync();
                validatedRequest.ClientIds = await UserSession.GetClientListAsync();
            }

            LogSuccess(validatedRequest);

            return new EndSessionValidationResult
            {
                ValidatedRequest = validatedRequest,
                IsError = false
            };
        }

        /// <summary>
        /// Creates a result that indicates an error.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual EndSessionValidationResult Invalid(string message, ValidatedEndSessionRequest request = null)
        {
            message = "End session request validation failure: " + message;
            if (request != null)
            {
                var log = new EndSessionRequestValidationLog(request);
                Logger.LogInformation(message + Environment.NewLine + "{@details}", log);
            }
            else
            {
                Logger.LogInformation(message);
            }

            return new EndSessionValidationResult
            {
                IsError = true,
                Error = "Invalid request",
                ErrorDescription = message
            };
        }

        /// <summary>
        /// Logs a success result.
        /// </summary>
        /// <param name="request"></param>
        protected virtual void LogSuccess(ValidatedEndSessionRequest request)
        {
            var log = new EndSessionRequestValidationLog(request);
            Logger.LogInformation("End session request validation success" + Environment.NewLine + "{@details}", log);
        }

        /// <inheritdoc />
        public async Task<EndSessionCallbackValidationResult> ValidateCallbackAsync(NameValueCollection parameters)
        {
            var result = new EndSessionCallbackValidationResult
            {
                IsError = true
            };

            var endSessionId = parameters[Constants.UIConstants.DefaultRoutePathParams.EndSessionCallback];
            var endSessionMessage = await EndSessionMessageStore.ReadAsync(endSessionId);
            if (endSessionMessage?.Data?.ClientIds?.Any() == true)
            {
                result.IsError = false;

                var (frontChannel, backChannel) = await GetClientEndSessionUrlsAsync(endSessionMessage.Data);
                result.FrontChannelLogoutUrls = frontChannel;
                result.BackChannelLogouts = backChannel;
            }

            return result;
        }

        /// <summary>
        /// Creates the data structures for front-channel and back-channel sign-out notifications.
        /// </summary>
        /// <param name="endSession"></param>
        /// <returns></returns>
        protected virtual async Task<(IEnumerable<string> frontChannel, IEnumerable<BackChannelLogoutModel> backChannel)> GetClientEndSessionUrlsAsync(EndSession endSession)
        {
            var frontChannelUrls = new List<string>();
            var backChannelLogouts = new List<BackChannelLogoutModel>();
            foreach (var clientId in endSession.ClientIds)
            {
                var client = await ClientStore.FindEnabledClientByIdAsync(clientId);
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
                                url = url.AddQueryString(OidcConstants.EndSessionRequest.Issuer, Context.HttpContext.GetIdentityServerIssuerUri());
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
                Logger.LogDebug("Client front-channel logout URLs: {0}", msg);
            }
            else
            {
                Logger.LogDebug("No client front-channel logout URLs");
            }

            if (backChannelLogouts.Any())
            {
                var msg = backChannelLogouts.Select(x => x.LogoutUri).Aggregate((x, y) => x + ", " + y);
                Logger.LogDebug("Client back-channel logout URLs: {0}", msg);
            }
            else
            {
                Logger.LogDebug("No client back-channel logout URLs");
            }

            return (frontChannelUrls, backChannelLogouts);
        }
    }
}