// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IdentityServer4.Endpoints.Results;
using IdentityServer4.Hosting;
using IdentityServer4.Infrastructure;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Endpoints
{
    internal class EndSessionCallbackEndpoint : IEndpointHandler
    {
        private readonly IEndSessionRequestValidator _endSessionRequestValidator;
        private readonly BackChannelLogoutClient _backChannelClient;
        private readonly ILogger _logger;

        public EndSessionCallbackEndpoint(
            IEndSessionRequestValidator endSessionRequestValidator,
            BackChannelLogoutClient backChannelClient,
            ILogger<EndSessionCallbackEndpoint> logger)
        {
            _endSessionRequestValidator = endSessionRequestValidator;
            _backChannelClient = backChannelClient;
            _logger = logger;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext context)
        {
            if (!HttpMethods.IsGet(context.Request.Method))
            {
                _logger.LogWarning("Invalid HTTP method for end session callback endpoint.");
                return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
            }

            _logger.LogDebug("Processing signout callback request");

            var parameters = context.Request.Query.AsNameValueCollection();
            var result = await _endSessionRequestValidator.ValidateCallbackAsync(parameters);

            if (result.IsError == false)
            {
                _logger.LogInformation("Successful signout callback.");

                if (result.FrontChannelLogoutUrls?.Any() == true)
                {
                    _logger.LogDebug("Client front-channel iframe urls: {urls}", result.FrontChannelLogoutUrls);
                }
                else
                {
                    _logger.LogDebug("No client front-channel iframe urls");
                }

                if (result.BackChannelLogouts?.Any() == true)
                {

                    _logger.LogDebug("Client back-channel iframe urls: {urls}", result.BackChannelLogouts.Select(x=>x.LogoutUri));
                }
                else
                {
                    _logger.LogDebug("No client back-channel iframe urls");
                }

                await InvokeBackChannelClientsAsync(result);
            }

            return new EndSessionCallbackResult(result);
        }

        private async Task InvokeBackChannelClientsAsync(EndSessionCallbackValidationResult result)
        {
            if (result.BackChannelLogouts?.Any() == true)
            {
                // best-effort, and async to not block the response to the browser
                try
                {
                    await _backChannelClient.SendLogoutsAsync(result.BackChannelLogouts);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calling backchannel sign-out urls");
                }
            }
        }
    }
}
