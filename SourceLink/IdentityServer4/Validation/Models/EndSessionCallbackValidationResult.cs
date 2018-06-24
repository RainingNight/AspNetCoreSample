// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;

namespace IdentityServer4.Validation
{
    /// <summary>
    /// Validation result for end session callback requests.
    /// </summary>
    /// <seealso cref="IdentityServer4.Validation.ValidationResult" />
    public class EndSessionCallbackValidationResult : ValidationResult
    {
        /// <summary>
        /// Gets the client front-channel logout urls.
        /// </summary>
        public IEnumerable<string> FrontChannelLogoutUrls { get; set; }
        
        /// <summary>
        /// Gets the client back-channel logouts.
        /// </summary>
        public IEnumerable<BackChannelLogoutModel> BackChannelLogouts { get; set; }
    }

    /// <summary>
    /// Information necessary to make a back-channel logout request to a client.
    /// </summary>
    public class BackChannelLogoutModel
    {
        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets the subject identifier.
        /// </summary>
        public string SubjectId { get; set; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the back channel logout URI.
        /// </summary>
        public string LogoutUri { get; set; }

        /// <summary>
        /// Gets a value indicating whether the session identifier is required.
        /// </summary>
        public bool SessionIdRequired { get; set; }
    }
}