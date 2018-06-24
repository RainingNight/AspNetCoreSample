// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Infrastructure
{
    internal class BackChannelLogoutClient
    {
        private const int LogoutTokenLifetime = 5 * 60;

        //private readonly IHttpContextAccessor _httpContext;
        private readonly ISystemClock _clock;
        private readonly IdentityServerTools _tools;
        private readonly BackChannelHttpClient _backChannelClient;
        private readonly ILogger<BackChannelLogoutClient> _logger;

        public BackChannelLogoutClient(
            //IHttpContextAccessor httpContext,
            ISystemClock clock,
            IdentityServerTools tools,
            BackChannelHttpClient backChannelClient,
            ILogger<BackChannelLogoutClient> logger)
        {
            //_httpContext = httpContext;
            _clock = clock;
            _tools = tools;
            _backChannelClient = backChannelClient;
            _logger = logger;
        }

        public virtual Task SendLogoutsAsync(IEnumerable<BackChannelLogoutModel> clients)
        {
            clients = clients ?? Enumerable.Empty<BackChannelLogoutModel>();
            var tasks = clients.Select(x => SendLogoutAsync(x)).ToArray();
            return Task.WhenAll(tasks);
        }

        private async Task SendLogoutAsync(BackChannelLogoutModel client)
        {
            var token = await CreateLogoutTokenAsync(client);

            var data = new Dictionary<string, string>
            {
                { OidcConstants.BackChannelLogoutRequest.LogoutToken, token }
            };

            try
            {
                var response = await _backChannelClient.PostAsync(client.LogoutUri, new FormUrlEncodedContent(data));

                _logger.LogDebug("Back channel logout for client id: {0} to URI: {1}, result: {2}",
                    client.ClientId, client.LogoutUri, (int)response.StatusCode);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception invoking back channel logout for client id: {0} to URI: {1}", client.ClientId, client.LogoutUri);
            }
        }

        private async Task<string> CreateLogoutTokenAsync(BackChannelLogoutModel client)
        {
            var json = "{\"" + OidcConstants.Events.BackChannelLogout + "\":{} }";
            var claims = new List<Claim>
            {
                //new Claim(JwtClaimTypes.Issuer, _httpContext.HttpContext.GetIdentityServerIssuerUri()),
                new Claim(JwtClaimTypes.Subject, client.SubjectId),
                new Claim(JwtClaimTypes.Audience, client.ClientId),
                new Claim(JwtClaimTypes.IssuedAt, _clock.UtcNow.UtcDateTime.ToEpochTime().ToString(), ClaimValueTypes.Integer),
                new Claim(JwtClaimTypes.JwtId, CryptoRandom.CreateUniqueId(16)),
                new Claim(JwtClaimTypes.Events, json, IdentityServerConstants.ClaimValueTypes.Json)
            };

            if (client.SessionIdRequired)
            {
                claims.Add(new Claim(JwtClaimTypes.SessionId, client.SessionId));
            }

            return await _tools.IssueJwtAsync(LogoutTokenLifetime, claims);
        }
    }
}
