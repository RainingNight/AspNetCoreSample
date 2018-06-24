// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Linq;

namespace IdentityServer4.Models
{
    /// <summary>
    /// Extensions for Resource
    /// </summary>
    public static class ResourceExtensions
    {
        /// <summary>
        /// Converts to scope names.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <returns></returns>
        public static IEnumerable<string> ToScopeNames(this Resources resources)
        {
            var scopes = from api in resources.ApiResources
                         where api.Scopes != null
                         from scope in api.Scopes
                         select scope.Name;
            if (resources.OfflineAccess)
            {
                scopes = scopes.Union(new[] { IdentityServerConstants.StandardScopes.OfflineAccess });
            }
            return resources.IdentityResources.Select(x => x.Name).Union(scopes).ToArray();
        }

        /// <summary>
        /// Finds the API resource by scope.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static ApiResource FindApiResourceByScope(this Resources resources, string name)
        {
            var q = from api in resources.ApiResources
                    where api.Scopes != null
                    from scope in api.Scopes
                    where scope.Name == name
                    select api;
            return q.FirstOrDefault();
        }

        /// <summary>
        /// Finds the API scope.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static Scope FindApiScope(this Resources resources, string name)
        {
            var q = from api in resources.ApiResources
                    where api.Scopes != null
                    from scope in api.Scopes
                    where scope.Name == name
                    select scope;
            return q.FirstOrDefault();
        }

        /// <summary>
        /// Finds the API scope.
        /// </summary>
        /// <param name="api">The API.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static Scope FindApiScope(this ApiResource api, string name)
        {
            if (api == null || api.Scopes == null) return null;

            var q = from s in api.Scopes
                    where s.Name == name
                    select s;
            return q.FirstOrDefault();
        }

        internal static Resources FilterEnabled(this Resources resources)
        {
            if (resources == null) return new Resources();

            var identity = resources.IdentityResources.Where(x => x.Enabled);

            var api = from a in resources.ApiResources
                      where a.Enabled
                      select a;

            return new Resources(identity, api)
            {
                OfflineAccess = resources.OfflineAccess
            };
        }
    }
}