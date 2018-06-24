// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Hosting;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Pipeline extension methods for adding IdentityServer
    /// </summary>
    public static class IdentityServerApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds IdentityServer to the pipeline.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseIdentityServer(this IApplicationBuilder app)
        {
            app.Validate();

            app.UseMiddleware<BaseUrlMiddleware>();

            app.ConfigureCors();

            // it seems ok if we have UseAuthentication more than once in the pipeline --
            // this will just re-run the various callback handlers and the default authN 
            // handler, which just re-assigns the user on the context. claims transformation
            // will run twice, since that's not cached (whereas the authN handler result is)
            // related: https://github.com/aspnet/Security/issues/1399
            app.UseAuthentication();

            app.UseMiddleware<IdentityServerMiddleware>();

            return app;
        }

        internal static void Validate(this IApplicationBuilder app)
        {
            var loggerFactory = app.ApplicationServices.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            var logger = loggerFactory.CreateLogger("IdentityServer4.Startup");

            var scopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;

                TestService(serviceProvider, typeof(IPersistedGrantStore), logger, "No storage mechanism for grants specified. Use the 'AddInMemoryPersistedGrants' extension method to register a development version.");
                TestService(serviceProvider, typeof(IClientStore), logger, "No storage mechanism for clients specified. Use the 'AddInMemoryClients' extension method to register a development version.");
                TestService(serviceProvider, typeof(IResourceStore), logger, "No storage mechanism for resources specified. Use the 'AddInMemoryIdentityResources' or 'AddInMemoryApiResources' extension method to register a development version.");

                var persistedGrants = serviceProvider.GetService(typeof(IPersistedGrantStore));
                if (persistedGrants.GetType().FullName == typeof(InMemoryPersistedGrantStore).FullName)
                {
                    logger.LogInformation("You are using the in-memory version of the persisted grant store. This will store consent decisions, authorization codes, refresh and reference tokens in memory only. If you are using any of those features in production, you want to switch to a different store implementation.");
                }

                var options = serviceProvider.GetRequiredService<IdentityServerOptions>();
                ValidateOptions(options, logger);

                ValidateAsync(serviceProvider, logger).GetAwaiter().GetResult();
            }
        }

        private static async Task ValidateAsync(IServiceProvider services, ILogger logger)
        {
            var schemes = services.GetRequiredService<IAuthenticationSchemeProvider>();

            if (await schemes.GetDefaultAuthenticateSchemeAsync() == null)
            {
                logger.LogWarning("No default authentication scheme has been set. Setting a default scheme is required.");
            }
            else
            {
                logger.LogDebug("Using {scheme} as default scheme for authentication", (await schemes.GetDefaultAuthenticateSchemeAsync())?.Name);
                logger.LogDebug("Using {scheme} as default scheme for sign-in", (await schemes.GetDefaultSignInSchemeAsync())?.Name);
                logger.LogDebug("Using {scheme} as default scheme for sign-out", (await schemes.GetDefaultSignOutSchemeAsync())?.Name);
                logger.LogDebug("Using {scheme} as default scheme for challenge", (await schemes.GetDefaultChallengeSchemeAsync())?.Name);
                logger.LogDebug("Using {scheme} as default scheme for forbid", (await schemes.GetDefaultForbidSchemeAsync())?.Name);
            }
        }

        private static void ValidateOptions(IdentityServerOptions options, ILogger logger)
        {
            if (options.IssuerUri.IsPresent()) logger.LogDebug("Custom IssuerUri set to {0}", options.IssuerUri);

            if (options.PublicOrigin.IsPresent())
            {
                if (!Uri.TryCreate(options.PublicOrigin, UriKind.Absolute, out var uri))
                {
                    throw new InvalidOperationException($"PublicOrigin is not valid: {options.PublicOrigin}");
                }

                logger.LogDebug("PublicOrigin explicitly set to {0}", options.PublicOrigin);
            }

            if (options.UserInteraction.LoginUrl.IsMissing()) throw new InvalidOperationException("LoginUrl is not configured");
            if (options.UserInteraction.LoginReturnUrlParameter.IsMissing()) throw new InvalidOperationException("LoginReturnUrlParameter is not configured");
            if (options.UserInteraction.LogoutUrl.IsMissing()) throw new InvalidOperationException("LogoutUrl is not configured");
            if (options.UserInteraction.LogoutIdParameter.IsMissing()) throw new InvalidOperationException("LogoutIdParameter is not configured");
            if (options.UserInteraction.ErrorUrl.IsMissing()) throw new InvalidOperationException("ErrorUrl is not configured");
            if (options.UserInteraction.ErrorIdParameter.IsMissing()) throw new InvalidOperationException("ErrorIdParameter is not configured");
            if (options.UserInteraction.ConsentUrl.IsMissing()) throw new InvalidOperationException("ConsentUrl is not configured");
            if (options.UserInteraction.ConsentReturnUrlParameter.IsMissing()) throw new InvalidOperationException("ConsentReturnUrlParameter is not configured");
            if (options.UserInteraction.CustomRedirectReturnUrlParameter.IsMissing()) throw new InvalidOperationException("CustomRedirectReturnUrlParameter is not configured");

            if (options.Authentication.CheckSessionCookieName.IsMissing()) throw new InvalidOperationException("CheckSessionCookieName is not configured");

            if (options.Cors.CorsPolicyName.IsMissing()) throw new InvalidOperationException("CorsPolicyName is not configured");
        }

        internal static object TestService(IServiceProvider serviceProvider, Type service, ILogger logger, string message = null, bool doThrow = true)
        {
            var appService = serviceProvider.GetService(service);

            if (appService == null)
            {
                var error = message ?? $"Required service {service.FullName} is not registered in the DI container. Aborting startup";

                logger.LogCritical(error);

                if (doThrow)
                {
                    throw new InvalidOperationException(error);
                }
            }

            return appService;
        }
    }
}
