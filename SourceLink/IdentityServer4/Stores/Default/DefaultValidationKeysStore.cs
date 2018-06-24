// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

namespace IdentityServer4.Stores
{
    /// <summary>
    /// The default validation key store
    /// </summary>
    /// <seealso cref="IdentityServer4.Stores.IValidationKeysStore" />
    public class DefaultValidationKeysStore : IValidationKeysStore
    {
        private readonly IEnumerable<SecurityKey> _keys;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultValidationKeysStore"/> class.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <exception cref="System.ArgumentNullException">keys</exception>
        public DefaultValidationKeysStore(IEnumerable<SecurityKey> keys)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));

            _keys = keys;
        }

        /// <summary>
        /// Gets all validation keys.
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<SecurityKey>> GetValidationKeysAsync()
        {
            return Task.FromResult(_keys);
        }
    }
}