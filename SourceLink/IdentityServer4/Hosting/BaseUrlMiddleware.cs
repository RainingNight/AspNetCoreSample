// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using IdentityServer4.Configuration;

#pragma warning disable 1591

namespace IdentityServer4.Hosting
{
    public class BaseUrlMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IdentityServerOptions _options;

        public BaseUrlMiddleware(RequestDelegate next, IdentityServerOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;

            if (_options.PublicOrigin.IsPresent())
            {
                context.SetIdentityServerOrigin(_options.PublicOrigin);
            }

            context.SetIdentityServerBasePath(request.PathBase.Value.RemoveTrailingSlash());

            await _next(context);
        }
    }
}