// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Endpoints
{
    internal class AuthorizeCallbackEndpoint : AuthorizeEndpointBase
    {
        private readonly IConsentMessageStore _consentResponseStore;

        public AuthorizeCallbackEndpoint(
            IEventService events,
            ILogger<AuthorizeCallbackEndpoint> logger,
            IAuthorizeRequestValidator validator,
            IAuthorizeInteractionResponseGenerator interactionGenerator,
            IAuthorizeResponseGenerator authorizeResponseGenerator,
            IUserSession userSession,
            IConsentMessageStore consentResponseStore)
            : base(events, logger, validator, interactionGenerator, authorizeResponseGenerator, userSession)
        {
            _consentResponseStore = consentResponseStore;
        }

        public override async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            if (!HttpMethods.IsGet(context.Request.Method))
            {
                Logger.LogWarning("Invalid HTTP method for authorize endpoint.");
                return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
            }

            Logger.LogDebug("Start authorize callback request");

            var parameters = context.Request.Query.AsNameValueCollection();

            var user = await UserSession.GetUserAsync();
            var consentRequest = new ConsentRequest(parameters, user?.GetSubjectId());
            var consent = await _consentResponseStore.ReadAsync(consentRequest.Id);

            if (consent != null && consent.Data == null)
            {
                return await CreateErrorResultAsync("consent message is missing data");
            }

            try
            {
                var result = await ProcessAuthorizeRequestAsync(parameters, user, consent?.Data);

                Logger.LogTrace("End Authorize Request. Result type: {0}", result?.GetType().ToString() ?? "-none-");

                return result;
            }
            finally
            {
                if (consent != null)
                {
                    await _consentResponseStore.DeleteAsync(consentRequest.Id);
                }
            }
        }
    }
}
