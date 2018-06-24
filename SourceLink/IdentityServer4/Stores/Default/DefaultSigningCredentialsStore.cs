// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;

namespace IdentityServer4.Stores
{
    /// <summary>
    /// Default signing credentials store
    /// </summary>
    /// <seealso cref="IdentityServer4.Stores.ISigningCredentialStore" />
    public class DefaultSigningCredentialsStore : ISigningCredentialStore
    {
        private readonly SigningCredentials _credential;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSigningCredentialsStore"/> class.
        /// </summary>
        /// <param name="credential">The credential.</param>
        public DefaultSigningCredentialsStore(SigningCredentials credential)
        {
            _credential = credential;
        }

        /// <summary>
        /// Gets the signing credentials.
        /// </summary>
        /// <returns></returns>
        public Task<SigningCredentials> GetSigningCredentialsAsync()
        {
            return Task.FromResult(_credential);
        }
    }
}