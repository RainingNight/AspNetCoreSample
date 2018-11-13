// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace IdentityServer4.Configuration
{
    /// <summary>
    /// The IdentityServerOptions class is the top level container for all configuration settings of IdentityServer.
    /// </summary>
    public class IdentityServerOptions
    {
        /// <summary>
        /// Gets or sets the unique name of this server instance, e.g. https://myissuer.com.
        /// If not set, the issuer name is inferred from the request
        /// </summary>
        /// <value>
        /// Unique name of this server instance, e.g. https://myissuer.com
        /// </value>
        public string IssuerUri { get; set; }

        /// <summary>
        /// Gets or sets the origin of this server instance, e.g. https://myorigin.com.
        /// If not set, the origin name is inferred from the request
        /// Note: Do not set a URL or include a path.
        /// </summary>
        /// <value>
        /// Origin of this server instance, e.g. https://myorigin.com
        /// </value>
        public string PublicOrigin { get; set; }

        /// <summary>
        /// Gets or sets the endpoint configuration.
        /// </summary>
        /// <value>
        /// The endpoints configuration.
        /// </value>
        public EndpointsOptions Endpoints { get; set; } = new EndpointsOptions();

        /// <summary>
        /// Gets or sets the discovery endpoint configuration.
        /// </summary>
        /// <value>
        /// The discovery endpoint configuration.
        /// </value>
        public DiscoveryOptions Discovery { get; set; } = new DiscoveryOptions();

        /// <summary>
        /// Gets or sets the authentication options.
        /// </summary>
        /// <value>
        /// The authentication options.
        /// </value>
        public AuthenticationOptions Authentication { get; set; } = new AuthenticationOptions();

        /// <summary>
        /// Gets or sets the events options.
        /// </summary>
        /// <value>
        /// The events options.
        /// </value>
        public EventsOptions Events { get; set; } = new EventsOptions();

        /// <summary>
        /// Gets or sets the max input length restrictions.
        /// </summary>
        /// <value>
        /// The length restrictions.
        /// </value>
        public InputLengthRestrictions InputLengthRestrictions { get; set; } = new InputLengthRestrictions();

        /// <summary>
        /// Gets or sets the options for the user interaction.
        /// </summary>
        /// <value>
        /// The user interaction options.
        /// </value>
        public UserInteractionOptions UserInteraction { get; set; } = new UserInteractionOptions();

        /// <summary>
        /// Gets or sets the caching options.
        /// </summary>
        /// <value>
        /// The caching options.
        /// </value>
        public CachingOptions Caching { get; set; } = new CachingOptions();

        /// <summary>
        /// Gets or sets the cors options.
        /// </summary>
        /// <value>
        /// The cors options.
        /// </value>
        public CorsOptions Cors { get; set; } = new CorsOptions();

        /// <summary>
        /// Gets or sets the Content Security Policy options.
        /// </summary>
        public CspOptions Csp { get; set; } = new CspOptions();

        /// <summary>
        /// Gets or sets the validation options.
        /// </summary>
        public ValidationOptions Validation { get; set; } = new ValidationOptions();

        /// <summary>
        /// Gets or sets the device flow options.
        /// </summary>
        public DeviceFlowOptions DeviceFlow { get; set; } = new DeviceFlowOptions();
    }
}