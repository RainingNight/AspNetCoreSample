// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;

namespace IdentityServer4.Configuration
{
    /// <summary>
    /// Configures the login and logout views and behavior.
    /// </summary>
    public class AuthenticationOptions
    {
        /// <summary>
        /// Sets the cookie authenitcation scheme confgured by the host used for interactive users. If not set, the scheme will inferred from the host's default authentication scheme.
        /// This setting is typically used when AddPolicyScheme is used in the host as the default scheme.
        /// </summary>
        public string CookieAuthenticationScheme { get; set; }

        /// <summary>
        /// Sets the cookie lifetime (only effective if the IdentityServer-provided cookie handler is used)
        /// </summary>
        public TimeSpan CookieLifetime { get; set; } = Constants.DefaultCookieTimeSpan;

        /// <summary>
        /// Specified if the cookie should be sliding or not (only effective if the built-in cookie middleware is used)
        /// </summary>
        public bool CookieSlidingExpiration { get; set; } = false;

        /// <summary>
        /// Indicates if user must be authenticated to accept parameters to end session endpoint. Defaults to false.
        /// </summary>
        /// <value>
        /// <c>true</c> if required; otherwise, <c>false</c>.
        /// </value>
        public bool RequireAuthenticatedUserForSignOutMessage { get; set; } = false;

        /// <summary>
        /// Gets or sets the name of the cookie used for the check session endpoint.
        /// </summary>
        public string CheckSessionCookieName { get; set; } = IdentityServerConstants.DefaultCheckSessionCookieName;

        /// <summary>
        /// Gets or sets the timeout on the back channel logout HTTP call.
        /// </summary>
        public TimeSpan BackChannelLogoutTimeOut { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// If set, will require frame-src CSP headers being emitting on the end session callback endpoint which renders iframes to clients for front-channel signout notification.
        /// </summary>
        public bool RequireCspFrameSrcForSignout { get; set; } = true;
    }
}