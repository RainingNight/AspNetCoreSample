// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace IdentityServer4.Services
{
    /// <summary>
    /// Default refresh token service
    /// </summary>
    public class DefaultRefreshTokenService : IRefreshTokenService
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The refresh token store
        /// </summary>
        protected readonly IRefreshTokenStore RefreshTokenStore;

        /// <summary>
        /// The clock
        /// </summary>
        protected readonly ISystemClock Clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRefreshTokenService" /> class.
        /// </summary>
        /// <param name="clock">The clock</param>
        /// <param name="refreshTokenStore">The refresh token store</param>
        /// <param name="logger">The logger</param>
        public DefaultRefreshTokenService(ISystemClock clock, IRefreshTokenStore refreshTokenStore, ILogger<DefaultRefreshTokenService> logger)
        {
            Clock = clock;
            _logger = logger;
            RefreshTokenStore = refreshTokenStore;
        }

        /// <summary>
        /// Creates the refresh token.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="client">The client.</param>
        /// <returns>
        /// The refresh token handle
        /// </returns>
        public virtual async Task<string> CreateRefreshTokenAsync(ClaimsPrincipal subject, Token accessToken, Client client)
        {
            _logger.LogDebug("Creating refresh token");

            int lifetime;
            if (client.RefreshTokenExpiration == TokenExpiration.Absolute)
            {
                _logger.LogDebug("Setting an absolute lifetime: " + client.AbsoluteRefreshTokenLifetime);
                lifetime = client.AbsoluteRefreshTokenLifetime;
            }
            else
            {
                _logger.LogDebug("Setting a sliding lifetime: " + client.SlidingRefreshTokenLifetime);
                lifetime = client.SlidingRefreshTokenLifetime;
            }

            var refreshToken = new RefreshToken
            {
                CreationTime = Clock.UtcNow.UtcDateTime,
                Lifetime = lifetime,
                AccessToken = accessToken
            };

            var handle = await RefreshTokenStore.StoreRefreshTokenAsync(refreshToken);
            return handle;
        }

        /// <summary>
        /// Updates the refresh token.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="client">The client.</param>
        /// <returns>
        /// The refresh token handle
        /// </returns>
        public virtual async Task<string> UpdateRefreshTokenAsync(string handle, RefreshToken refreshToken, Client client)
        {
            _logger.LogDebug("Updating refresh token");

            bool needsCreate = false;
            bool needsUpdate = false;

            if (client.RefreshTokenUsage == TokenUsage.OneTimeOnly)
            {
                _logger.LogDebug("Token usage is one-time only. Generating new handle");

                // delete old one
                await RefreshTokenStore.RemoveRefreshTokenAsync(handle);

                // create new one
                needsCreate = true;
            }

            if (client.RefreshTokenExpiration == TokenExpiration.Sliding)
            {
                _logger.LogDebug("Refresh token expiration is sliding - extending lifetime");

                // if absolute exp > 0, make sure we don't exceed absolute exp
                // if absolute exp = 0, allow indefinite slide
                var currentLifetime = refreshToken.CreationTime.GetLifetimeInSeconds(Clock.UtcNow.UtcDateTime);
                _logger.LogDebug("Current lifetime: " + currentLifetime.ToString());

                var newLifetime = currentLifetime + client.SlidingRefreshTokenLifetime;
                _logger.LogDebug("New lifetime: " + newLifetime.ToString());

                // zero absolute refresh token lifetime represents unbounded absolute lifetime
                // if absolute lifetime > 0, cap at absolute lifetime
                if (client.AbsoluteRefreshTokenLifetime > 0 && newLifetime > client.AbsoluteRefreshTokenLifetime)
                {
                    newLifetime = client.AbsoluteRefreshTokenLifetime;
                    _logger.LogDebug("New lifetime exceeds absolute lifetime, capping it to " + newLifetime.ToString());
                }

                refreshToken.Lifetime = newLifetime;
                needsUpdate = true;
            }

            if (needsCreate)
            {
                handle = await RefreshTokenStore.StoreRefreshTokenAsync(refreshToken);
                _logger.LogDebug("Created refresh token in store");
            }
            else if (needsUpdate)
            {
                await RefreshTokenStore.UpdateRefreshTokenAsync(handle, refreshToken);
                _logger.LogDebug("Updated refresh token in store");
            }
            else
            {
                _logger.LogDebug("No updates to refresh token done");
            }

            return handle;
        }
    }
}