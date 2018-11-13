// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Microsoft.Extensions.Primitives;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using IdentityModel;

namespace IdentityServer4.Endpoints.Results
{
    internal class ProtectedResourceErrorResult : IEndpointResult
    {
        public string Error;
        public string ErrorDescription;

        public ProtectedResourceErrorResult(string error, string errorDescription = null)
        {
            Error = error;
            ErrorDescription = errorDescription;
        }

        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.SetNoCache();

            if (Constants.ProtectedResourceErrorStatusCodes.ContainsKey(Error))
            {
                context.Response.StatusCode = Constants.ProtectedResourceErrorStatusCodes[Error];
            }

            if (Error == OidcConstants.ProtectedResourceErrors.ExpiredToken)
            {
                Error = OidcConstants.ProtectedResourceErrors.InvalidToken;
                ErrorDescription = "The access token expired";
            }

            var errorString = string.Format($"error=\"{Error}\"");
            if (ErrorDescription.IsMissing())
            {
                context.Response.Headers.Add(HeaderNames.WWWAuthenticate, new StringValues(new[] { "Bearer", errorString }));
            }
            else
            {
                var errorDescriptionString = string.Format($"error_description=\"{ErrorDescription}\"");
                context.Response.Headers.Add(HeaderNames.WWWAuthenticate, new StringValues(new[] { "Bearer", errorString, errorDescriptionString }));
            }

            return Task.CompletedTask;
        }
    }
}