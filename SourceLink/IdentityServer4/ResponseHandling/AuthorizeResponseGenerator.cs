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
    /// The authorize response generator
    /// </summary>
    /// <seealso cref="IdentityServer4.ResponseHandling.IAuthorizeResponseGenerator" />
    public class AuthorizeResponseGenerator : IAuthorizeResponseGenerator
    {
        /// <summary>
        /// The token service
        /// </summary>
        protected readonly ITokenService TokenService;

        /// <summary>
        /// The authorization code store
        /// </summary>
        protected readonly IAuthorizationCodeStore AuthorizationCodeStore;

        /// <summary>
        /// The event service
        /// </summary>
        protected readonly IEventService Events;

        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The clock
        /// </summary>
        protected readonly ISystemClock Clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeResponseGenerator"/> class.
        /// </summary>
        /// <param name="clock">The clock.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="tokenService">The token service.</param>
        /// <param name="authorizationCodeStore">The authorization code store.</param>
        /// <param name="events">The events.</param>
        public AuthorizeResponseGenerator(ISystemClock clock, ITokenService tokenService, IAuthorizationCodeStore authorizationCodeStore, ILogger<AuthorizeResponseGenerator> logger, IEventService events)
        {
            Clock = clock;
            TokenService = tokenService;
            AuthorizationCodeStore = authorizationCodeStore;
            Events = events;
            Logger = logger;
        }

        /// <summary>
        /// Creates the response
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">invalid grant type: " + request.GrantType</exception>
        public virtual async Task<AuthorizeResponse> CreateResponseAsync(ValidatedAuthorizeRequest request)
        {
            if (request.GrantType == GrantType.AuthorizationCode)
            {
                return await CreateCodeFlowResponseAsync(request);
            }
            if (request.GrantType == GrantType.Implicit)
            {
                return await CreateImplicitFlowResponseAsync(request);
            }
            if (request.GrantType == GrantType.Hybrid)
            {
                return await CreateHybridFlowResponseAsync(request);
            }

            Logger.LogError("Unsupported grant type: " + request.GrantType);
            throw new InvalidOperationException("invalid grant type: " + request.GrantType);
        }

        /// <summary>
        /// Creates the response for a hybrid flow request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<AuthorizeResponse> CreateHybridFlowResponseAsync(ValidatedAuthorizeRequest request)
        {
            Logger.LogDebug("Creating Hybrid Flow response.");

            var code = await CreateCodeAsync(request);
            var response = await CreateImplicitFlowResponseAsync(request, code);
            response.Code = code;

            return response;
        }

        /// <summary>
        /// Creates the response for a code flow request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected virtual async Task<AuthorizeResponse> CreateCodeFlowResponseAsync(ValidatedAuthorizeRequest request)
        {
            Logger.LogDebug("Creating Authorization Code Flow response.");

            var code = await CreateCodeAsync(request);

            var response = new AuthorizeResponse
            {
                Request = request,
                Code = code,
                SessionState = request.GenerateSessionStateValue()
            };

            return response;
        }

        /// <summary>
        /// Creates the response for a implicit flow request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="authorizationCode"></param>
        /// <returns></returns>
        protected virtual async Task<AuthorizeResponse> CreateImplicitFlowResponseAsync(ValidatedAuthorizeRequest request, string authorizationCode = null)
        {
            Logger.LogDebug("Creating Implicit Flow response.");

            string accessTokenValue = null;
            int accessTokenLifetime = 0;

            var responseTypes = request.ResponseType.FromSpaceSeparatedString();

            if (responseTypes.Contains(OidcConstants.ResponseTypes.Token))
            {
                var tokenRequest = new TokenCreationRequest
                {
                    Subject = request.Subject,
                    Resources = request.ValidatedScopes.GrantedResources,

                    ValidatedRequest = request
                };

                var accessToken = await TokenService.CreateAccessTokenAsync(tokenRequest);
                accessTokenLifetime = accessToken.Lifetime;

                accessTokenValue = await TokenService.CreateSecurityTokenAsync(accessToken);
            }

            string jwt = null;
            if (responseTypes.Contains(OidcConstants.ResponseTypes.IdToken))
            {
                var tokenRequest = new TokenCreationRequest
                {
                    ValidatedRequest = request,
                    Subject = request.Subject,
                    Resources = request.ValidatedScopes.GrantedResources,

                    Nonce = request.Raw.Get(OidcConstants.AuthorizeRequest.Nonce),
                    // if no access token is requested, then we need to include all the claims in the id token
                    IncludeAllIdentityClaims = !request.AccessTokenRequested,
                    AccessTokenToHash = accessTokenValue,
                    AuthorizationCodeToHash = authorizationCode
                };

                var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);
                jwt = await TokenService.CreateSecurityTokenAsync(idToken);
            }

            var response = new AuthorizeResponse
            {
                Request = request,
                AccessToken = accessTokenValue,
                AccessTokenLifetime = accessTokenLifetime,
                IdentityToken = jwt,
                SessionState = request.GenerateSessionStateValue()
            };

            return response;
        }

        /// <summary>
        /// Creates an authorization code
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected async Task<string> CreateCodeAsync(ValidatedAuthorizeRequest request)
        {
            var code = new AuthorizationCode
            {
                CreationTime = Clock.UtcNow.UtcDateTime,
                ClientId = request.Client.ClientId,
                Lifetime = request.Client.AuthorizationCodeLifetime,
                Subject = request.Subject,
                SessionId = request.SessionId,
                CodeChallenge = request.CodeChallenge.Sha256(),
                CodeChallengeMethod = request.CodeChallengeMethod,

                IsOpenId = request.IsOpenIdRequest,
                RequestedScopes = request.ValidatedScopes.GrantedResources.ToScopeNames(),
                RedirectUri = request.RedirectUri,
                Nonce = request.Nonce,

                WasConsentShown = request.WasConsentShown
            };

            // store id token and access token and return authorization code
            var id = await AuthorizationCodeStore.StoreAuthorizationCodeAsync(code);

            return id;
        }
   }
}