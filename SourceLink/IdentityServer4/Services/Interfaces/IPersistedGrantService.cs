// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer4.Services
{
    /// <summary>
    /// Implements persisted grant logic
    /// </summary>
    public interface IPersistedGrantService
    {
        /// <summary>
        /// Gets all grants for a given subject ID.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <returns></returns>
        Task<IEnumerable<Consent>> GetAllGrantsAsync(string subjectId);

        /// <summary>
        /// Removes all grants for a given subject id and client id combination.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns></returns>
        Task RemoveAllGrantsAsync(string subjectId, string clientId);
    }
}