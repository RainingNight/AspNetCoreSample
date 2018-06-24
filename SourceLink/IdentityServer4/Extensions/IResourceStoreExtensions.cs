// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IdentityServer4.Stores
{
    /// <summary>
    /// Extensions for IResourceStore
    /// </summary>
    public static class IResourceStoreExtensions
    {
        /// <summary>
        /// Finds the resources by scope.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="scopeNames">The scope names.</param>
        /// <returns></returns>
        public static async Task<Resources> FindResourcesByScopeAsync(this IResourceStore store, IEnumerable<string> scopeNames)
        {
            var identity = await store.FindIdentityResourcesByScopeAsync(scopeNames);
            var apiResources = await store.FindApiResourcesByScopeAsync(scopeNames);

            Validate(identity, apiResources);

            var apis = new List<ApiResource>();
            foreach (var apiResource in apiResources)
            {
                apis.Add(apiResource.CloneWithScopes(apiResource.Scopes.Where(x => scopeNames.Contains(x.Name))));
            }

            var resources = new Resources(identity, apis)
            {
                OfflineAccess = scopeNames.Contains(IdentityServerConstants.StandardScopes.OfflineAccess)
            };
            return resources;
        }

        private static void Validate(IEnumerable<IdentityResource> identity, IEnumerable<ApiResource> apiResources)
        {
            // attempt to detect invalid configuration. this is about the only place
            // we can do this, since it's hard to get the values in the store.
            var identityScopeNames = identity.Select(x => x.Name).ToArray();
            var apiScopeNames = (from api in apiResources
                                 from scope in api.Scopes
                                 select scope.Name).ToArray();
            CheckForDuplicates(identityScopeNames, apiScopeNames);

            var overlap = identityScopeNames.Intersect(apiScopeNames).ToArray();
            if (overlap.Any())
            {
                var names = overlap.Aggregate((x, y) => x + ", " + y);
                throw new Exception(String.Format("Found identity scopes and API scopes that use the same names. This is an invalid configuration. Use different names for identity scopes and API scopes. Scopes found: {0}", names));
            }
        }

        private static void CheckForDuplicates(string[] identityScopeNames, string[] apiScopeNames)
        {
            var identityDuplicates = identityScopeNames
                            .GroupBy(x => x)
                            .Where(g => g.Count() > 1)
                            .Select(y => y.Key)
                            .ToArray();
            if (identityDuplicates.Any())
            {
                var names = identityDuplicates.Aggregate((x, y) => x + ", " + y);
                throw new Exception(String.Format("Duplicate identity scopes found. This is an invalid configuration. Use different names for identity scopes. Scopes found: {0}", names));
            }

            var apiDuplicates = apiScopeNames
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .ToArray();
            if (apiDuplicates.Any())
            {
                var names = apiDuplicates.Aggregate((x, y) => x + ", " + y);
                throw new Exception(String.Format("Duplicate API scopes found. This is an invalid configuration. Use different names for API scopes. Scopes found: {0}", names));
            }
        }

        /// <summary>
        /// Finds the enabled resources by scope.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="scopeNames">The scope names.</param>
        /// <returns></returns>
        public static async Task<Resources> FindEnabledResourcesByScopeAsync(this IResourceStore store, IEnumerable<string> scopeNames)
        {
            return (await store.FindResourcesByScopeAsync(scopeNames)).FilterEnabled();
        }

        /// <summary>
        /// Gets all enabled resources.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <returns></returns>
        public static async Task<Resources> GetAllEnabledResourcesAsync(this IResourceStore store)
        {
            var resources = await store.GetAllResourcesAsync();
            Validate(resources.IdentityResources, resources.ApiResources);

            return resources.FilterEnabled();
        }

        /// <summary>
        /// Finds the enabled identity resources by scope.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="scopeNames">The scope names.</param>
        /// <returns></returns>
        public static async Task<IEnumerable<IdentityResource>> FindEnabledIdentityResourcesByScopeAsync(this IResourceStore store, IEnumerable<string> scopeNames)
        {
            return (await store.FindIdentityResourcesByScopeAsync(scopeNames)).Where(x => x.Enabled).ToArray();
        }
    }
}
