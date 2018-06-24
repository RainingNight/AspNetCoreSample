// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using System.Diagnostics;

namespace IdentityServer4.Models
{
    /// <summary>
    /// Models the common data of API and identity resources.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public abstract class Resource
    {
        /// <summary>
        /// Indicates if this resource is enabled. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The unique name of the resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Display name of the resource.
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Description of the resource.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// List of accociated user claims that should be included when this resource is requested.
        /// </summary>
        public ICollection<string> UserClaims { get; set; } = new HashSet<string>();
    }
}