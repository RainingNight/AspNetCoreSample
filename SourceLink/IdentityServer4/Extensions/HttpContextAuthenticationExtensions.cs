// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Linq;
using IdentityServer4.Configuration;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Extension methods for signin/out using the IdentityServer authentication scheme.
    /// </summary>
    public static class AuthenticationManagerExtensions
    {
        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, string subject, params Claim[] claims)
        {
            var clock = context.GetClock();

            var user = new IdentityServerUser(subject)
            {
                AdditionalClaims = claims,
                AuthenticationTime = clock.UtcNow.UtcDateTime
            };

            await context.SignInAsync(user);
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="properties">The authentication properties</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, string subject, AuthenticationProperties properties, params Claim[] claims)
        {
            var clock = context.GetClock();

            var user = new IdentityServerUser(subject)
            {
                AdditionalClaims = claims,
                AuthenticationTime = clock.UtcNow.UtcDateTime
            };

            await context.SignInAsync(user, properties);
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, string subject, string name, params Claim[] claims)
        {
            var clock = context.GetClock();

            var user = new IdentityServerUser(subject)
            {
                DisplayName = name,
                AdditionalClaims = claims,
                AuthenticationTime = clock.UtcNow.UtcDateTime
            };

            await context.SignInAsync(user);
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, string subject, string name, AuthenticationProperties properties, params Claim[] claims)
        {
            var clock = context.GetClock();

            var user = new IdentityServerUser(subject)
            {
                DisplayName = name,
                AdditionalClaims = claims,
                AuthenticationTime = clock.UtcNow.UtcDateTime
            };

            await context.SignInAsync(user, properties);
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="identityProvider">The identity provider.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, string subject, string name, string identityProvider, params Claim[] claims)
        {
            var clock = context.GetClock();

            var user = new IdentityServerUser(subject)
            {
                DisplayName = name,
                IdentityProvider = identityProvider,
                AdditionalClaims = claims,
                AuthenticationTime = clock.UtcNow.UtcDateTime
            };

            await context.SignInAsync(user);
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="identityProvider">The identity provider.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, string subject, string name, string identityProvider, AuthenticationProperties properties, params Claim[] claims)
        {
            var clock = context.GetClock();

            var user = new IdentityServerUser(subject)
            {
                DisplayName = name,
                IdentityProvider = identityProvider,
                AdditionalClaims = claims,
                AuthenticationTime = clock.UtcNow.UtcDateTime
            };

            await context.SignInAsync(user, properties);
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="authenticationMethods">The authentication methods.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, string subject, string name, IEnumerable<string> authenticationMethods, params Claim[] claims)
        {
            var clock = context.GetClock();

            var user = new IdentityServerUser(subject)
            {
                DisplayName = name,
                AuthenticationMethods = authenticationMethods.ToList(),
                AdditionalClaims = claims,
                AuthenticationTime = clock.UtcNow.UtcDateTime
            };

            await context.SignInAsync(user);
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="authenticationMethods">The authentication methods.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, string subject, string name, IEnumerable<string> authenticationMethods, AuthenticationProperties properties, params Claim[] claims)
        {
            var clock = context.GetClock();

            var user = new IdentityServerUser(subject)
            {
                DisplayName = name,
                AuthenticationMethods = authenticationMethods.ToList(),
                AdditionalClaims = claims,
                AuthenticationTime = clock.UtcNow.UtcDateTime
            };

            await context.SignInAsync(user, properties);
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="sub">The sub.</param>
        /// <param name="name">The name.</param>
        /// <param name="identityProvider">The identity provider.</param>
        /// <param name="authenticationMethods">The authentication methods.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, string sub, string name, string identityProvider, IEnumerable<string> authenticationMethods, params Claim[] claims)
        {
            var clock = context.GetClock();

            var user = new IdentityServerUser(sub)
            {
                DisplayName = name,
                IdentityProvider = identityProvider,
                AuthenticationMethods = authenticationMethods.ToList(),
                AdditionalClaims = claims,
                AuthenticationTime = clock.UtcNow.UtcDateTime
            };

            await context.SignInAsync(user);
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="sub">The sub.</param>
        /// <param name="name">The name.</param>
        /// <param name="identityProvider">The identity provider.</param>
        /// <param name="authenticationMethods">The authentication methods.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, string sub, string name, string identityProvider, IEnumerable<string> authenticationMethods, AuthenticationProperties properties, params Claim[] claims)
        {
            var clock = context.GetClock();

            var user = new IdentityServerUser(sub)
            {
                DisplayName = name,
                IdentityProvider = identityProvider,
                AuthenticationMethods = authenticationMethods.ToList(),
                AdditionalClaims = claims,
                AuthenticationTime = clock.UtcNow.UtcDateTime
            };

            await context.SignInAsync(user, properties);
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="user">The IdentityServer user.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, IdentityServerUser user)
        {
            await context.SignInAsync(await context.GetCookieAuthenticationSchemeAsync(), user.CreatePrincipal());
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="context">The manager.</param>
        /// <param name="user">The IdentityServer user.</param>
        /// <param name="properties">The authentication properties.</param>
        /// <returns></returns>
        public static async Task SignInAsync(this HttpContext context, IdentityServerUser user, AuthenticationProperties properties)
        {
            await context.SignInAsync(await context.GetCookieAuthenticationSchemeAsync(), user.CreatePrincipal(), properties);
        }

        internal static ISystemClock GetClock(this HttpContext context)
        {
            return context.RequestServices.GetRequiredService<ISystemClock>();
        }

        internal static async Task<string> GetCookieAuthenticationSchemeAsync(this HttpContext context)
        {
            var options = context.RequestServices.GetRequiredService<IdentityServerOptions>();
            if (options.Authentication.CookieAuthenticationScheme != null)
            {
                return options.Authentication.CookieAuthenticationScheme;
            }

            var schemes = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemes.GetDefaultAuthenticateSchemeAsync();
            if (scheme == null)
            {
                throw new InvalidOperationException($"No DefaultAuthenticateScheme found or no CookieAuthenticationScheme configured on IdentityServerOptions.");
            }

            return scheme.Name;
        }
    }
}