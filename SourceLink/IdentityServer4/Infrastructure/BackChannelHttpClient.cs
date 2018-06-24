// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Net.Http;
using IdentityServer4.Configuration;

namespace IdentityServer4.Infrastructure
{
    /// <summary>
    /// Used to model back-channel HTTP calls for back-channel logout spec.
    /// </summary>
    /// <seealso cref="System.Net.Http.HttpClient" />
    public class BackChannelHttpClient : HttpClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackChannelHttpClient"/> class.
        /// </summary>
        public BackChannelHttpClient()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackChannelHttpClient"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public BackChannelHttpClient(IdentityServerOptions options)
        {
            Timeout = options.Authentication.BackChannelLogoutTimeOut;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackChannelHttpClient"/> class.
        /// </summary>
        /// <param name="handler">The HTTP handler stack to use for sending requests.</param>
        public BackChannelHttpClient(HttpMessageHandler handler) : base(handler)
        {
        }
    }
}
