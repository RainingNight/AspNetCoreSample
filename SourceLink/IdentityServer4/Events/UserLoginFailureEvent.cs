// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using static IdentityServer4.Constants;

namespace IdentityServer4.Events
{
    /// <summary>
    /// Event for failed user authentication
    /// </summary>
    /// <seealso cref="IdentityServer4.Events.Event" />
    public class UserLoginFailureEvent : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:IdentityServer4.Events.UserLoginFailureEvent" /> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="error">The error.</param>
        /// <param name="interactive">Specifies if login was interactive</param>
        public UserLoginFailureEvent(string username, string error, bool interactive = true)
            : base(EventCategories.Authentication,
                  "User Login Failure",
                  EventTypes.Failure, 
                  EventIds.UserLoginFailure,
                  error)
        {
            Username = username;

            if (interactive)
            {
                Endpoint = "UI";
            }
            else
            {
                Endpoint = EndpointNames.Token;
            }
        }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        /// <value>
        /// The endpoint.
        /// </value>
        public string Endpoint { get; set; }
    }
}
