// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace IdentityServer4
{
    /// <summary>
    /// Factory for IdentityServer compatible principals
    /// </summary>
    public static class IdentityServerPrincipal
    {
        /// <summary>
        /// Creates a principal.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        [Obsolete("This method will be removed in a future version, use IdentityServerUser instead to create the ClaimsPrincipal")]
        public static ClaimsPrincipal Create(
            string subject,
            string name,
            params Claim[] claims)
        {
            return Create(subject, name, DateTime.UtcNow, claims);
        }

        /// <summary>
        /// Creates a principal.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="authTime">The UTC date/time of authentication.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        [Obsolete("This method will be removed in a future version, use IdentityServerUser instead to create the ClaimsPrincipal")]
        public static ClaimsPrincipal Create(
            string subject,
            string name,
            DateTime authTime,
            params Claim[] claims)
        {
            return Create(subject, name, IdentityServerConstants.LocalIdentityProvider, new[] { OidcConstants.AuthenticationMethods.Password }, authTime, claims);
        }

        /// <summary>
        /// Creates a principal.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="identityProvider">The identity provider.</param>
        /// <param name="authTime">The UTC date/time of authentication.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        [Obsolete("This method will be removed in a future version, use IdentityServerUser instead to create the ClaimsPrincipal")]
        public static ClaimsPrincipal Create(
            string subject,
            string name,
            string identityProvider,
            DateTime authTime,
            params Claim[] claims)
        {
            return Create(subject, name, identityProvider, new[] { Constants.ExternalAuthenticationMethod }, authTime, claims);
        }

        /// <summary>
        /// Creates a principal.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="authenticationMethods">The authentication methods.</param>
        /// <param name="authTime">The UTC date/time of authentication.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        [Obsolete("This method will be removed in a future version, use IdentityServerUser instead to create the ClaimsPrincipal")]
        public static ClaimsPrincipal Create(
            string subject,
            string name,
            IEnumerable<string> authenticationMethods,
            DateTime authTime,
            params Claim[] claims)
        {
            return Create(subject, name, IdentityServerConstants.LocalIdentityProvider, authenticationMethods, authTime, claims);
        }

        /// <summary>
        /// Creates a principal.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="name">The name.</param>
        /// <param name="identityProvider">The identity provider.</param>
        /// <param name="authenticationMethods">The authentication methods.</param>
        /// <param name="authTime">The UTC date/time of authentication.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// subject
        /// or
        /// name
        /// or
        /// authenticationMethods
        /// or
        /// identityProvider
        /// </exception>
        [Obsolete("This method will be removed in a future version, use IdentityServerUser instead to create the ClaimsPrincipal")]
        public static ClaimsPrincipal Create(
            string subject,
            string name,
            string identityProvider,
            IEnumerable<string> authenticationMethods,
            DateTime authTime,
            params Claim[] claims)
        {
            if (subject.IsMissing()) throw new ArgumentNullException(nameof(subject));
            if (name.IsMissing()) throw new ArgumentNullException(nameof(name));
            if (authenticationMethods.IsNullOrEmpty()) throw new ArgumentNullException(nameof(authenticationMethods));
            if (identityProvider.IsMissing()) throw new ArgumentNullException(nameof(identityProvider));
            
            var allClaims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, subject),
                new Claim(JwtClaimTypes.Name, name),
                new Claim(JwtClaimTypes.IdentityProvider, identityProvider),
                new Claim(JwtClaimTypes.AuthenticationTime, authTime.ToEpochTime().ToString(), ClaimValueTypes.Integer)
            };

            foreach (var amr in authenticationMethods)
            {
                allClaims.Add(new Claim(JwtClaimTypes.AuthenticationMethod, amr));
            }

            foreach (var claim in claims)
            {
                allClaims.Add(claim);
            }

            var id = new ClaimsIdentity(allClaims.Distinct(new ClaimComparer()), Constants.IdentityServerAuthenticationType, JwtClaimTypes.Name, JwtClaimTypes.Role);
            return new ClaimsPrincipal(id);
        }

        internal static ClaimsPrincipal FromSubjectId(string subjectId, IEnumerable<Claim> additionalClaims = null)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, subjectId)
            };

            if (additionalClaims != null)
            {
                claims.AddRange(additionalClaims);
            }

            return Principal.Create(Constants.IdentityServerAuthenticationType,
                claims.Distinct(new ClaimComparer()).ToArray());
        }
    }
}