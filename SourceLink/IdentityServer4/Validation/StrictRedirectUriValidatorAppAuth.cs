// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace IdentityServer4.Validation
{
    /// <summary>
    /// Implementation of strict redirect URI validator that allows a random port if 127.0.0.1 is used.
    /// </summary>
    /// <seealso cref="IdentityServer4.Validation.StrictRedirectUriValidator" />
    public class StrictRedirectUriValidatorAppAuth : StrictRedirectUriValidator
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StrictRedirectUriValidatorAppAuth"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public StrictRedirectUriValidatorAppAuth(ILogger<StrictRedirectUriValidatorAppAuth> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Determines whether a redirect URI is valid for a client.
        /// </summary>
        /// <param name="requestedUri">The requested URI.</param>
        /// <param name="client">The client.</param>
        /// <returns>
        /// <c>true</c> is the URI is valid; <c>false</c> otherwise.
        /// </returns>
        public override async Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
        {
            var isAllowed = await base.IsRedirectUriValidAsync(requestedUri, client);
            if (isAllowed) return isAllowed;

            // since this is appauth specific, we can require pkce
            if (client.RequirePkce && client.RedirectUris.Contains("http://127.0.0.1")) return IsLoopback(requestedUri);

            return false;
        }

        /// <summary>
        /// Determines whether a post logout URI is valid for a client.
        /// </summary>
        /// <param name="requestedUri">The requested URI.</param>
        /// <param name="client">The client.</param>
        /// <returns>
        /// <c>true</c> is the URI is valid; <c>false</c> otherwise.
        /// </returns>
        public override async Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
        {
            var isAllowed = await base.IsPostLogoutRedirectUriValidAsync(requestedUri, client);
            if (isAllowed) return isAllowed;

            // since this is appauth specific, we can require pkce
            if (client.RequirePkce && client.RedirectUris.Contains("http://127.0.0.1")) return IsLoopback(requestedUri);

            return false;
        }

        // check if http://127.0.0.1:random_port is used
        internal bool IsLoopback(string requestedUri)
        {
            _logger.LogDebug("Checking for 127.0.0.1 redirect URI");

            var parts = requestedUri.Split(':');

            if (parts.Length != 3)
            {
                _logger.LogDebug("invalid format - http://127.0.0.1:port is required.");
                return false;
            }

            if (!string.Equals(parts[0], "http", StringComparison.Ordinal) ||
                !string.Equals(parts[1], "//127.0.0.1", StringComparison.Ordinal))
            {
                _logger.LogDebug("invalid format - http://127.0.0.1:port is required.");
                return false;
            }

            if (int.TryParse(parts[2], out var port))
            {
                if (port >= 0 && port <= 65536) return true;
            }

            _logger.LogDebug("invalid port");
            return false;
        }
    }
}