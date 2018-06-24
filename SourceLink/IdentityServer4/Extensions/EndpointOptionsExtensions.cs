// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Configuration;
using IdentityServer4.Hosting;
using static IdentityServer4.Constants;

namespace IdentityServer4.Extensions
{
    internal static class EndpointOptionsExtensions
    {
        public static bool IsEndpointEnabled(this EndpointsOptions options, Endpoint endpoint)
        {
            switch (endpoint?.Name)
            {
                case EndpointNames.Authorize:
                    return options.EnableAuthorizeEndpoint;
                case EndpointNames.CheckSession:
                    return options.EnableCheckSessionEndpoint;
                case EndpointNames.Discovery:
                    return options.EnableDiscoveryEndpoint;
                case EndpointNames.EndSession:
                    return options.EnableEndSessionEndpoint;
                case EndpointNames.Introspection:
                    return options.EnableIntrospectionEndpoint;
                case EndpointNames.Revocation:
                    return options.EnableTokenRevocationEndpoint;
                case EndpointNames.Token:
                    return options.EnableTokenEndpoint;
                case EndpointNames.UserInfo:
                    return options.EnableUserInfoEndpoint;
                default:
                    // fall thru to true to allow custom endpoints
                    return true;
            }
        }
    }
}