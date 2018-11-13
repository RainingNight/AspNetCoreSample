// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;

namespace IdentityServer4.Models
{
    /// <summary>
    /// Represents contextual information about a device flow authorization request.
    /// </summary>
    public class DeviceFlowAuthorizationRequest
    {
        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>
        /// The client identifier.
        /// </value>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the scopes requested.
        /// </summary>
        /// <value>
        /// The scopes requested.
        /// </value>
        public IEnumerable<string> ScopesRequested { get; set; }
    }
}