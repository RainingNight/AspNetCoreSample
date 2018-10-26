// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer4.Services
{
    /// <summary>
    /// Interface for the key material service
    /// </summary>
    public interface IKeyMaterialService
    {
        /// <summary>
        /// Gets all validation keys.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<SecurityKey>> GetValidationKeysAsync();

        /// <summary>
        /// Gets the signing credentials.
        /// </summary>
        /// <returns></returns>
        Task<SigningCredentials> GetSigningCredentialsAsync();
    }
}