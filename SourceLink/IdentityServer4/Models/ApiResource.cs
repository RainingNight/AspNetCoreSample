// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServer4.Models
{
    /// <summary>
    /// Models a web API resource.
    /// </summary>
    public class ApiResource : Resource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResource"/> class.
        /// </summary>
        public ApiResource()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResource"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ApiResource(string name)
            : this(name, name, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResource"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="displayName">The display name.</param>
        public ApiResource(string name, string displayName)
            : this(name, displayName, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResource"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="claimTypes">The claim types.</param>
        public ApiResource(string name, IEnumerable<string> claimTypes)
            : this(name, name, claimTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResource"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="displayName">The display name.</param>
        /// <param name="claimTypes">The claim types.</param>
        /// <exception cref="System.ArgumentNullException">name</exception>
        public ApiResource(string name, string displayName, IEnumerable<string> claimTypes)
        {
            if (name.IsMissing()) throw new ArgumentNullException(nameof(name));

            Name = name;
            DisplayName = displayName;

            Scopes.Add(new Scope(name, displayName));

            if (!claimTypes.IsNullOrEmpty())
            {
                foreach (var type in claimTypes)
                {
                    UserClaims.Add(type);
                }
            }
        }

        /// <summary>
        /// The API secret is used for the introspection endpoint. The API can authenticate with introspection using the API name and secret.
        /// </summary>
        public ICollection<Secret> ApiSecrets { get; set; } = new HashSet<Secret>();

        /// <summary>
        /// An API must have at least one scope. Each scope can have different settings.
        /// </summary>
        public ICollection<Scope> Scopes { get; set; } = new HashSet<Scope>();

        internal ApiResource CloneWithScopes(IEnumerable<Scope> scopes)
        {
            return new ApiResource
            {
                Enabled = Enabled,
                Name = Name,
                ApiSecrets = ApiSecrets,
                Scopes = new HashSet<Scope>(scopes.ToArray()),
                UserClaims = UserClaims
            };
        }
    }
}
