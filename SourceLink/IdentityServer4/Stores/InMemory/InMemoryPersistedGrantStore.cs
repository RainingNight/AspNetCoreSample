// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IdentityServer4.Stores
{
    /// <summary>
    /// In-memory persisted grant store
    /// </summary>
    public class InMemoryPersistedGrantStore : IPersistedGrantStore
    {
        private readonly ConcurrentDictionary<string, PersistedGrant> _repository = new ConcurrentDictionary<string, PersistedGrant>();

        /// <summary>
        /// Stores the grant.
        /// </summary>
        /// <param name="grant">The grant.</param>
        /// <returns></returns>
        public Task StoreAsync(PersistedGrant grant)
        {
            _repository[grant.Key] = grant;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the grant.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public Task<PersistedGrant> GetAsync(string key)
        {
            PersistedGrant token;
            if (_repository.TryGetValue(key, out token))
            {
                return Task.FromResult(token);
            }

            return Task.FromResult<PersistedGrant>(null);
        }

        /// <summary>
        /// Gets all grants for a given subject id.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <returns></returns>
        public Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var query =
                from item in _repository
                where item.Value.SubjectId == subjectId
                select item.Value;

            var items = query.ToArray().AsEnumerable();
            return Task.FromResult(items);
        }

        /// <summary>
        /// Removes the grant by key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public Task RemoveAsync(string key)
        {
            _repository.TryRemove(key, out _);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes all grants for a given subject id and client id combination.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns></returns>
        public Task RemoveAllAsync(string subjectId, string clientId)
        {
            var query =
                from item in _repository
                where item.Value.ClientId == clientId &&
                    item.Value.SubjectId == subjectId
                select item.Key;

            var keys = query.ToArray();
            foreach (var key in keys)
            {
                _repository.TryRemove(key, out _);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes all grants of a give type for a given subject id and client id combination.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            var query =
                from item in _repository
                where item.Value.SubjectId == subjectId &&
                    item.Value.ClientId == clientId &&
                    item.Value.Type == type
                select item.Key;

            var keys = query.ToArray();
            foreach (var key in keys)
            {
                _repository.TryRemove(key, out _);
            }

            return Task.CompletedTask;
        }
    }
}