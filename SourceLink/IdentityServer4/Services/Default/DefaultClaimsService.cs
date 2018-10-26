// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer4.Services
{
    /// <summary>
    /// Default claims provider implementation
    /// </summary>
    public class DefaultClaimsService : IClaimsService
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// The user service
        /// </summary>
        protected readonly IProfileService Profile;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultClaimsService"/> class.
        /// </summary>
        /// <param name="profile">The profile service</param>
        /// <param name="logger">The logger</param>
        public DefaultClaimsService(IProfileService profile, ILogger<DefaultClaimsService> logger)
        {
            Logger = logger;
            Profile = profile;
        }

        /// <summary>
        /// Returns claims for an identity token
        /// </summary>
        /// <param name="subject">The subject</param>
        /// <param name="resources">The requested resources</param>
        /// <param name="includeAllIdentityClaims">Specifies if all claims should be included in the token, or if the userinfo endpoint can be used to retrieve them</param>
        /// <param name="request">The raw request</param>
        /// <returns>
        /// Claims for the identity token
        /// </returns>
        public virtual async Task<IEnumerable<Claim>> GetIdentityTokenClaimsAsync(ClaimsPrincipal subject, Resources resources, bool includeAllIdentityClaims, ValidatedRequest request)
        {
            Logger.LogDebug("Getting claims for identity token for subject: {subject} and client: {clientId}",
                subject.GetSubjectId(),
                request.Client.ClientId);

            var outputClaims = new List<Claim>(GetStandardSubjectClaims(subject));
            outputClaims.AddRange(GetOptionalClaims(subject));

            // fetch all identity claims that need to go into the id token
            if (includeAllIdentityClaims || request.Client.AlwaysIncludeUserClaimsInIdToken)
            {
                var additionalClaimTypes = new List<string>();

                foreach (var identityResource in resources.IdentityResources)
                {
                    foreach (var userClaim in identityResource.UserClaims)
                    {
                        additionalClaimTypes.Add(userClaim);
                    }
                }

                // filter so we don't ask for claim types that we will eventually filter out
                additionalClaimTypes = FilterRequestedClaimTypes(additionalClaimTypes).ToList();

                var context = new ProfileDataRequestContext(
                    subject,
                    request.Client,
                    IdentityServerConstants.ProfileDataCallers.ClaimsProviderIdentityToken,
                    additionalClaimTypes)
                {
                    RequestedResources = resources,
                    ValidatedRequest = request
                };

                await Profile.GetProfileDataAsync(context);

                var claims = FilterProtocolClaims(context.IssuedClaims);
                if (claims != null)
                {
                    outputClaims.AddRange(claims);
                }
            }
            else
            {
                Logger.LogDebug("In addition to an id_token, an access_token was requested. No claims other than sub are included in the id_token. To obtain more user claims, either use the user info endpoint or set AlwaysIncludeUserClaimsInIdToken on the client configuration.");
            }

            return outputClaims;
        }

        /// <summary>
        /// Returns claims for an identity token.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <param name="resources">The requested resources</param>
        /// <param name="request">The raw request.</param>
        /// <returns>
        /// Claims for the access token
        /// </returns>
        public virtual async Task<IEnumerable<Claim>> GetAccessTokenClaimsAsync(ClaimsPrincipal subject, Resources resources, ValidatedRequest request)
        {
            Logger.LogDebug("Getting claims for access token for client: {clientId}", request.Client.ClientId);

            // add client_id
            var outputClaims = new List<Claim>
            {
                new Claim(JwtClaimTypes.ClientId, request.Client.ClientId)
            };

            // add cnf if present
            if (request.Confirmation.IsPresent())
            {
                outputClaims.Add(new Claim(JwtClaimTypes.Confirmation, request.Confirmation, IdentityServerConstants.ClaimValueTypes.Json));
            }

            // check for client claims
            if (request.ClientClaims != null && request.ClientClaims.Any())
            {
                if (subject == null || request.Client.AlwaysSendClientClaims)
                {
                    foreach (var claim in request.ClientClaims)
                    {
                        var claimType = claim.Type;

                        if (request.Client.ClientClaimsPrefix.IsPresent())
                        {
                            claimType = request.Client.ClientClaimsPrefix + claimType;
                        }

                        outputClaims.Add(new Claim(claimType, claim.Value, claim.ValueType));
                    }
                }
            }

            // add scopes
            foreach (var scope in resources.IdentityResources)
            {
                outputClaims.Add(new Claim(JwtClaimTypes.Scope, scope.Name));
            }
            foreach (var scope in resources.ApiResources.SelectMany(x => x.Scopes))
            {
                outputClaims.Add(new Claim(JwtClaimTypes.Scope, scope.Name));
            }

            // a user is involved
            if (subject != null)
            {
                if (resources.OfflineAccess)
                {
                    outputClaims.Add(new Claim(JwtClaimTypes.Scope, IdentityServerConstants.StandardScopes.OfflineAccess));
                }

                Logger.LogDebug("Getting claims for access token for subject: {subject}", subject.GetSubjectId());

                outputClaims.AddRange(GetStandardSubjectClaims(subject));
                outputClaims.AddRange(GetOptionalClaims(subject));

                // fetch all resource claims that need to go into the access token
                var additionalClaimTypes = new List<string>();
                foreach (var api in resources.ApiResources)
                {
                    // add claims configured on api resource
                    if (api.UserClaims != null)
                    {
                        foreach (var claim in api.UserClaims)
                        {
                            additionalClaimTypes.Add(claim);
                        }
                    }

                    // add claims configured on scope
                    foreach (var scope in api.Scopes)
                    {
                        if (scope.UserClaims != null)
                        {
                            foreach (var claim in scope.UserClaims)
                            {
                                additionalClaimTypes.Add(claim);
                            }
                        }
                    }
                }

                // filter so we don't ask for claim types that we will eventually filter out
                additionalClaimTypes = FilterRequestedClaimTypes(additionalClaimTypes).ToList();

                var context = new ProfileDataRequestContext(
                    subject,
                    request.Client,
                    IdentityServerConstants.ProfileDataCallers.ClaimsProviderAccessToken,
                    additionalClaimTypes.Distinct())
                {
                    RequestedResources = resources,
                    ValidatedRequest = request
                };

                await Profile.GetProfileDataAsync(context);

                var claims = FilterProtocolClaims(context.IssuedClaims);
                if (claims != null)
                {
                    outputClaims.AddRange(claims);
                }
            }

            return outputClaims;
        }

        /// <summary>
        /// Gets the standard subject claims.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <returns>A list of standard claims</returns>
        protected virtual IEnumerable<Claim> GetStandardSubjectClaims(ClaimsPrincipal subject)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, subject.GetSubjectId()),
                new Claim(JwtClaimTypes.AuthenticationTime, subject.GetAuthenticationTimeEpoch().ToString(), ClaimValueTypes.Integer),
                new Claim(JwtClaimTypes.IdentityProvider, subject.GetIdentityProvider())
            };

            claims.AddRange(subject.GetAuthenticationMethods());

            return claims;
        }

        /// <summary>
        /// Gets additional (and optional) claims from the cookie or incoming subject.
        /// </summary>
        /// <param name="subject">The subject.</param>
        /// <returns>Additional claims</returns>
        protected virtual IEnumerable<Claim> GetOptionalClaims(ClaimsPrincipal subject)
        {
            var claims = new List<Claim>();

            var acr = subject.FindFirst(JwtClaimTypes.AuthenticationContextClassReference);
            if (acr != null) claims.Add(acr);

            return claims;
        }

        /// <summary>
        /// Filters out protocol claims like amr, nonce etc..
        /// </summary>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        protected virtual IEnumerable<Claim> FilterProtocolClaims(IEnumerable<Claim> claims)
        {
            var claimsToFilter = claims.Where(x => Constants.Filters.ClaimsServiceFilterClaimTypes.Contains(x.Type));
            if (claimsToFilter.Any())
            {
                var types = claimsToFilter.Select(x => x.Type);
                Logger.LogDebug("Claim types from profile service that were filtered: {claimTypes}", types);
            }
            return claims.Except(claimsToFilter);
        }

        /// <summary>
        /// Filters out protocol claims like amr, nonce etc..
        /// </summary>
        /// <param name="claimTypes">The claim types.</param>
        protected virtual IEnumerable<string> FilterRequestedClaimTypes(IEnumerable<string> claimTypes)
        {
            var claimTypesToFilter = claimTypes.Where(x => Constants.Filters.ClaimsServiceFilterClaimTypes.Contains(x));
            return claimTypes.Except(claimTypesToFilter);
        }
    }
}