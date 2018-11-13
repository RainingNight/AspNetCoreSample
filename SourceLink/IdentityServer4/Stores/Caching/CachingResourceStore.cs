// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using System.Threading.Tasks;
using IdentityServer4.Configuration;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Stores
{
    /// <summary>
    /// Caching decorator for IResourceStore
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IdentityServer4.Stores.IResourceStore" />
    public class CachingResourceStore<T> : IResourceStore
        where T : IResourceStore
    {
        private const string AllKey = "__all__";

        private readonly IdentityServerOptions _options;
        private readonly ICache<IEnumerable<IdentityResource>> _identityCache;
        private readonly ICache<IEnumerable<ApiResource>> _apiByScopeCache;
        private readonly ICache<ApiResource> _apiCache;
        private readonly ICache<Resources> _allCache;
        private readonly IResourceStore _inner;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingResourceStore{T}"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="identityCache">The identity cache.</param>
        /// <param name="apiByScopeCache">The API by scope cache.</param>
        /// <param name="apiCache">The API cache.</param>
        /// <param name="allCache">All cache.</param>
        /// <param name="logger">The logger.</param>
        public CachingResourceStore(IdentityServerOptions options, T inner, 
            ICache<IEnumerable<IdentityResource>> identityCache, 
            ICache<IEnumerable<ApiResource>> apiByScopeCache,
            ICache<ApiResource> apiCache,
            ICache<Resources> allCache,
            ILogger<CachingResourceStore<T>> logger)
        {
            _options = options;
            _inner = inner;
            _identityCache = identityCache;
            _apiByScopeCache = apiByScopeCache;
            _apiCache = apiCache;
            _allCache = allCache;
            _logger = logger;
        }

        private string GetKey(IEnumerable<string> names)
        {
            if (names == null || !names.Any()) return string.Empty;
            return names.OrderBy(x => x).Aggregate((x, y) => x + "," + y);
        }

        /// <summary>
        /// Gets all resources.
        /// </summary>
        /// <returns></returns>
        public async Task<Resources> GetAllResourcesAsync()
        {
            var key = AllKey;

            var all = await _allCache.GetAsync(key,
                _options.Caching.ResourceStoreExpiration,
                () => _inner.GetAllResourcesAsync(),
                _logger);

            return all;
        }

        /// <summary>
        /// Finds the API resource by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public async Task<ApiResource> FindApiResourceAsync(string name)
        {
            var api = await _apiCache.GetAsync(name,
                _options.Caching.ResourceStoreExpiration,
                () => _inner.FindApiResourceAsync(name),
                _logger);

            return api;
        }

        /// <summary>
        /// Finds the identity resources by scope.
        /// </summary>
        /// <param name="names">The names.</param>
        /// <returns></returns>
        public async Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> names)
        {
            var key = GetKey(names);

            var identities = await _identityCache.GetAsync(key,
                _options.Caching.ResourceStoreExpiration,
                () => _inner.FindIdentityResourcesByScopeAsync(names),
                _logger);

            return identities;
        }

        /// <summary>
        /// Finds the API resources by scope.
        /// </summary>
        /// <param name="names">The names.</param>
        /// <returns></returns>
        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> names)
        {
            var key = GetKey(names);

            var apis = await _apiByScopeCache.GetAsync(key,
                _options.Caching.ResourceStoreExpiration,
                () => _inner.FindApiResourcesByScopeAsync(names),
                _logger);

            return apis;
        }
    }
}