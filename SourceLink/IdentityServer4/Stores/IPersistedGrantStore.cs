// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer4.Stores
{
    /// <summary>
    /// Interface for persisting any type of grant.
    /// </summary>
    public interface IPersistedGrantStore
    {
        /// <summary>
        /// Stores the grant.
        /// </summary>
        /// <param name="grant">The grant.</param>
        /// <returns></returns>
        Task StoreAsync(PersistedGrant grant);

        /// <summary>
        /// Gets the grant.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        Task<PersistedGrant> GetAsync(string key);

        /// <summary>
        /// Gets all grants for a given subject id.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <returns></returns>
        Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId);

        /// <summary>
        /// Removes the grant by key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        Task RemoveAsync(string key);

        /// <summary>
        /// Removes all grants for a given subject id and client id combination.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns></returns>
        Task RemoveAllAsync(string subjectId, string clientId);

        /// <summary>
        /// Removes all grants of a give type for a given subject id and client id combination.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        Task RemoveAllAsync(string subjectId, string clientId, string type);
    }
}