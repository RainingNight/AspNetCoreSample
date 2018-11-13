// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Logging.Models;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Endpoints
{
    internal abstract class AuthorizeEndpointBase : IEndpointHandler
    {
        private readonly IAuthorizeResponseGenerator _authorizeResponseGenerator;

        private readonly IEventService _events;

        private readonly IAuthorizeInteractionResponseGenerator _interactionGenerator;

        private readonly IAuthorizeRequestValidator _validator;

        protected AuthorizeEndpointBase(
            IEventService events,
            ILogger<AuthorizeEndpointBase> logger,
            IAuthorizeRequestValidator validator,
            IAuthorizeInteractionResponseGenerator interactionGenerator,
            IAuthorizeResponseGenerator authorizeResponseGenerator,
            IUserSession userSession)
        {
            _events = events;
            Logger = logger;
            _validator = validator;
            _interactionGenerator = interactionGenerator;
            _authorizeResponseGenerator = authorizeResponseGenerator;
            UserSession = userSession;
        }

        protected ILogger Logger { get; private set; }

        protected IUserSession UserSession { get; private set; }

        public abstract Task<IEndpointResult> ProcessAsync(HttpContext context);

        internal async Task<IEndpointResult> ProcessAuthorizeRequestAsync(NameValueCollection parameters, ClaimsPrincipal user, ConsentResponse consent)
        {
            if (user != null)
            {
                Logger.LogDebug("User in authorize request: {subjectId}", user.GetSubjectId());
            }
            else
            {
                Logger.LogDebug("No user present in authorize request");
            }

            // validate request
            var result = await _validator.ValidateAsync(parameters, user);
            if (result.IsError)
            {
                return await CreateErrorResultAsync(
                    "Request validation failed",
                    result.ValidatedRequest,
                    result.Error,
                    result.ErrorDescription);
            }

            var request = result.ValidatedRequest;
            LogRequest(request);

            // determine user interaction
            var interactionResult = await _interactionGenerator.ProcessInteractionAsync(request, consent);
            if (interactionResult.IsError)
            {
                return await CreateErrorResultAsync("Interaction generator error", request, interactionResult.Error, logError: false);
            }
            if (interactionResult.IsLogin)
            {
                return new LoginPageResult(request);
            }
            if (interactionResult.IsConsent)
            {
                return new ConsentPageResult(request);
            }
            if (interactionResult.IsRedirect)
            {
                return new CustomRedirectResult(request, interactionResult.RedirectUrl);
            }

            var response = await _authorizeResponseGenerator.CreateResponseAsync(request);

            await RaiseResponseEventAsync(response);

            LogResponse(response);

            return new AuthorizeResult(response);
        }

        protected async Task<IEndpointResult> CreateErrorResultAsync(
            string logMessage,
            ValidatedAuthorizeRequest request = null,
            string error = OidcConstants.AuthorizeErrors.ServerError,
            string errorDescription = null,
            bool logError = true)
        {
            if (logError)
            {
                Logger.LogError(logMessage);
            }

            if (request != null)
            {
                var details = new AuthorizeRequestValidationLog(request);
                Logger.LogInformation("{@validationDetails}", details);
            }

            // TODO: should we raise a token failure event for all errors to the authorize endpoint?
            await RaiseFailureEventAsync(request, error, errorDescription);

            return new AuthorizeResult(new AuthorizeResponse
            {
                Request = request,
                Error = error,
                ErrorDescription = errorDescription
            });
        }

        private void LogRequest(ValidatedAuthorizeRequest request)
        {
            var details = new AuthorizeRequestValidationLog(request);
            Logger.LogInformation(nameof(ValidatedAuthorizeRequest) + Environment.NewLine + "{@validationDetails}", details);
        }

        private void LogResponse(AuthorizeResponse response)
        {
            var details = new AuthorizeResponseLog(response);
            Logger.LogInformation("Authorize endpoint response" + Environment.NewLine + "{@details}", details);
        }

        private void LogTokens(AuthorizeResponse response)
        {
            var clientId = $"{response.Request.ClientId} ({response.Request.Client.ClientName ?? "no name set"})";
            var subjectId = response.Request.Subject.GetSubjectId();

            if (response.IdentityToken != null)
            {
                Logger.LogTrace("Identity token issued for {clientId} / {subjectId}: {token}", clientId, subjectId, response.IdentityToken);
            }
            if (response.Code != null)
            {
                Logger.LogTrace("Code issued for {clientId} / {subjectId}: {token}", clientId, subjectId, response.Code);
            }
            if (response.AccessToken != null)
            {
                Logger.LogTrace("Access token issued for {clientId} / {subjectId}: {token}", clientId, subjectId, response.AccessToken);
            }
        }

        private Task RaiseFailureEventAsync(ValidatedAuthorizeRequest request, string error, string errorDescription)
        {
            return _events.RaiseAsync(new TokenIssuedFailureEvent(request, error, errorDescription));
        }

        private Task RaiseResponseEventAsync(AuthorizeResponse response)
        {
            if (!response.IsError)
            {
                LogTokens(response);
                return _events.RaiseAsync(new TokenIssuedSuccessEvent(response));
            }
            else
            {
                return RaiseFailureEventAsync(response.Request, response.Error, response.ErrorDescription);
            }
        }
    }
}