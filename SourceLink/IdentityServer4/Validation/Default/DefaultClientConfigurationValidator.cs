using IdentityServer4.Configuration;
using IdentityServer4.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Validation
{
    /// <summary>
    /// Default client configuration validator
    /// </summary>
    /// <seealso cref="IdentityServer4.Validation.IClientConfigurationValidator" />
    public class DefaultClientConfigurationValidator : IClientConfigurationValidator
    {
        private readonly IdentityServerOptions _options;

        // todo: default ctor for backwards compat; remove in 3.0

        /// <summary>
        /// Constructor for DefaultClientConfigurationValidator
        /// </summary>
        public DefaultClientConfigurationValidator()
        {
        }

        /// <summary>
        /// Constructor for DefaultClientConfigurationValidator
        /// </summary>
        public DefaultClientConfigurationValidator(IdentityServerOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Determines whether the configuration of a client is valid.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task ValidateAsync(ClientConfigurationValidationContext context)
        {
            await ValidateGrantTypesAsync(context);
            if (context.IsValid == false) return;

            await ValidateLifetimesAsync(context);
            if (context.IsValid == false) return;

            await ValidateRedirectUriAsync(context);
            if (context.IsValid == false) return;

            await ValidateUriSchemesAsync(context);
            if (context.IsValid == false) return;

            await ValidateSecretsAsync(context);
            if (context.IsValid == false) return;

            await ValidatePropertiesAsync(context);
            if (context.IsValid == false) return;
        }

        /// <summary>
        /// Validates grant type related configuration settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected virtual Task ValidateGrantTypesAsync(ClientConfigurationValidationContext context)
        {
            if (!context.Client.AllowedGrantTypes.Any())
            {
                context.SetError("no allowed grant type specified");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates lifetime related configuration settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected virtual Task ValidateLifetimesAsync(ClientConfigurationValidationContext context)
        {
            if (context.Client.AccessTokenLifetime <= 0)
            {
                context.SetError("access token lifetime is 0 or negative");
                return Task.CompletedTask;
            }

            if (context.Client.IdentityTokenLifetime <= 0)
            {
                context.SetError("identity token lifetime is 0 or negative");
                return Task.CompletedTask;
            }

            // 0 means unlimited lifetime
            if (context.Client.AbsoluteRefreshTokenLifetime < 0)
            {
                context.SetError("absolute refresh token lifetime is negative");
                return Task.CompletedTask;
            }

            // 0 might mean that sliding is disabled
            if (context.Client.SlidingRefreshTokenLifetime < 0)
            {
                context.SetError("sliding refresh token lifetime is negative");
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates redirect URI related configuration.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected virtual Task ValidateRedirectUriAsync(ClientConfigurationValidationContext context)
        {
            if (context.Client.AllowedGrantTypes.Contains(GrantType.AuthorizationCode) ||
                context.Client.AllowedGrantTypes.Contains(GrantType.Hybrid) ||
                context.Client.AllowedGrantTypes.Contains(GrantType.Implicit))
            {
                if (!context.Client.RedirectUris.Any())
                {
                    context.SetError("No redirect URI configured.");
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates that URI schemes is not in the list of invalid URI scheme prefixes, as controlled by the ValidationOptions.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual Task ValidateUriSchemesAsync(ClientConfigurationValidationContext context)
        {
            // todo: null check for backwards compat; remove in 3.0
            if (_options != null)
            {
                foreach (var uri in context.Client.RedirectUris)
                {
                    if (_options.Validation.InvalidRedirectUriPrefixes
                            .Any(scheme => uri.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)))
                    {
                        context.SetError($"RedirectUri '{uri}' uses invalid scheme. If this scheme should be allowed, then configure it via ValidationOptions.");
                    }
                }

                foreach (var uri in context.Client.PostLogoutRedirectUris)
                {
                    if (_options.Validation.InvalidRedirectUriPrefixes
                            .Any(scheme => uri.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)))
                    {
                        context.SetError($"PostLogoutRedirectUri '{uri}' uses invalid scheme. If this scheme should be allowed, then configure it via ValidationOptions.");
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates secret related configuration.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected virtual Task ValidateSecretsAsync(ClientConfigurationValidationContext context)
        {
            foreach (var grantType in context.Client.AllowedGrantTypes)
            {
                if (!string.Equals(grantType, GrantType.Implicit))
                {
                    if (context.Client.RequireClientSecret  && context.Client.ClientSecrets.Count == 0)
                    {
                        context.SetError($"Client secret is required for {grantType}, but no client secret is configured.");
                        return Task.CompletedTask;
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates properties related configuration settings.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        protected virtual Task ValidatePropertiesAsync(ClientConfigurationValidationContext context)
        {
            return Task.CompletedTask;
        }
    }
}