// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace IdentityServer4.Hosting
{
    internal class EndpointRouter : IEndpointRouter
    {
        private readonly IEnumerable<Endpoint> _endpoints;
        private readonly IdentityServerOptions _options;
        private readonly ILogger _logger;

        public EndpointRouter(IEnumerable<Endpoint> endpoints, IdentityServerOptions options, ILogger<EndpointRouter> logger)
        {
            _endpoints = endpoints;
            _options = options;
            _logger = logger;
        }

        public IEndpointHandler Find(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            foreach(var endpoint in _endpoints)
            {
                var path = endpoint.Path;
                if (context.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    var endpointName = endpoint.Name;
                    _logger.LogDebug("Request path {path} matched to endpoint type {endpoint}", context.Request.Path, endpointName);

                    return GetEndpointHandler(endpoint, context);
                }
            }

            _logger.LogTrace("No endpoint entry found for request path: {path}", context.Request.Path);

            return null;
        }

        private IEndpointHandler GetEndpointHandler(Endpoint endpoint, HttpContext context)
        {
            if (_options.Endpoints.IsEndpointEnabled(endpoint))
            {
                var handler = context.RequestServices.GetService(endpoint.Handler) as IEndpointHandler;
                if (handler != null)
                {
                    _logger.LogDebug("Endpoint enabled: {endpoint}, successfully created handler: {endpointHandler}", endpoint.Name, endpoint.Handler.FullName);
                    return handler;
                }
                else
                {
                    _logger.LogDebug("Endpoint enabled: {endpoint}, failed to create handler: {endpointHandler}", endpoint.Name, endpoint.Handler.FullName);
                }
            }
            else
            {
                _logger.LogWarning("Endpoint disabled: {endpoint}", endpoint.Name);
            }

            return null;
        }
    }
}
