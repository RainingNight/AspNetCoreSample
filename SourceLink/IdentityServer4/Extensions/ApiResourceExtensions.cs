// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Linq;

namespace IdentityServer4.Models
{
    internal static class ApiResourceExtensions
    {
        public static ApiResource CloneWithScopes(this ApiResource apiResource, IEnumerable<Scope> scopes)
        {
            return new ApiResource
            {
                Enabled = apiResource.Enabled,
                Name = apiResource.Name,
                DisplayName = apiResource.DisplayName,
                ApiSecrets = apiResource.ApiSecrets,
                Scopes = new HashSet<Scope>(scopes.ToArray()),
                UserClaims = apiResource.UserClaims
            };
        }
    }
}