// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using static IdentityServer4.Constants;

namespace IdentityServer4.Events
{
    /// <summary>
    /// Event for successful user authentication
    /// </summary>
    /// <seealso cref="IdentityServer4.Events.Event" />
    public class UserLoginSuccessEvent : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserLoginSuccessEvent"/> class.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="providerUserId">The provider user identifier.</param>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="name">The name.</param>
        public UserLoginSuccessEvent(string provider, string providerUserId, string subjectId, string name)
            : this()
        {
            Provider = provider;
            ProviderUserId = providerUserId;
            SubjectId = subjectId;
            DisplayName = name;
            Endpoint = "UI";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLoginSuccessEvent"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="subjectId">The subject identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="interactive">if set to <c>true</c> [interactive].</param>
        public UserLoginSuccessEvent(string username, string subjectId, string name, bool interactive = true)
            : this()
        {
            Username = username;
            SubjectId = subjectId;
            DisplayName = name;

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
        /// Initializes a new instance of the <see cref="UserLoginSuccessEvent"/> class.
        /// </summary>
        protected UserLoginSuccessEvent()
            : base(EventCategories.Authentication,
                  "User Login Success",
                  EventTypes.Success,
                  EventIds.UserLoginSuccess)
        {
        }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        /// <value>
        /// The provider.
        /// </value>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the provider user identifier.
        /// </summary>
        /// <value>
        /// The provider user identifier.
        /// </value>
        public string ProviderUserId { get; set; }

        /// <summary>
        /// Gets or sets the subject identifier.
        /// </summary>
        /// <value>
        /// The subject identifier.
        /// </value>
        public string SubjectId { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the endpoint.
        /// </summary>
        /// <value>
        /// The endpoint.
        /// </value>
        public string Endpoint { get; set; }
    }
}