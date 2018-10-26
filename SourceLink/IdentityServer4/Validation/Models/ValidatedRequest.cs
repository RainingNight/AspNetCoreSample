// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using System.Collections.Specialized;
using System.Security.Claims;
using IdentityModel;
using System.Linq;
using System;

namespace IdentityServer4.Validation
{
    /// <summary>
    /// Base class for a validate authorize or token request
    /// </summary>
    public class ValidatedRequest
    {
        /// <summary>
        /// Gets or sets the raw request data
        /// </summary>
        /// <value>
        /// The raw.
        /// </value>
        public NameValueCollection Raw { get; set; }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public Client Client { get; set; }

        /// <summary>
        /// Gets or sets the secret used to authenticate the client.
        /// </summary>
        /// <value>
        /// The parsed secret.
        /// </value>
        public ParsedSecret Secret { get; set; }

        /// <summary>
        /// Gets or sets the effective access token lifetime for the current request.
        /// This value is initally read from the client configuration but can be modified in the request pipeline
        /// </summary>
        public int AccessTokenLifetime { get; set; }

        /// <summary>
        /// Gets or sets the client claims for the current request.
        /// This value is initally read from the client configuration but can be modified in the request pipeline
        /// </summary>
        public ICollection<Claim> ClientClaims { get; set; } = new HashSet<Claim>(new ClaimComparer());

        /// <summary>
        /// Gets or sets the effective access token type for the current request.
        /// This value is initally read from the client configuration but can be modified in the request pipeline
        /// </summary>
        public AccessTokenType AccessTokenType { get; set; }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        /// <value>
        /// The subject.
        /// </value>
        public ClaimsPrincipal Subject { get; set; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        /// <value>
        /// The session identifier.
        /// </value>
        public string SessionId { get; set; }
        
        /// <summary>
        /// Gets or sets the identity server options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public IdentityServerOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the validated scopes.
        /// </summary>
        /// <value>
        /// The validated scopes.
        /// </value>
        public ScopeValidator ValidatedScopes { get; set; }

        /// <summary>
        /// Gets or sets the value of the confirmation method (will become the cnf claim). Must be a JSON object.
        /// </summary>
        /// <value>
        /// The confirmation.
        /// </value>
        public string Confirmation { get; set; }

        /// <summary>
        /// Sets the client and the appropriate request specific settings.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="secret">The client secret (optional).</param>
        /// <param name="confirmation">The confirmation.</param>
        /// <exception cref="ArgumentNullException">client</exception>
        public void SetClient(Client client, ParsedSecret secret = null, string confirmation = "")
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Secret = secret;
            Confirmation = confirmation;

            AccessTokenLifetime = client.AccessTokenLifetime;
            AccessTokenType = client.AccessTokenType;
            ClientClaims = client.Claims.Select(c => new Claim(c.Type, c.Value, c.ValueType, c.Issuer)).ToList();
        }
    }
}