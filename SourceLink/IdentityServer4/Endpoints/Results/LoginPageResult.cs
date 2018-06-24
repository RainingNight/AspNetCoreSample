// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Threading.Tasks;
using IdentityServer4.Hosting;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using IdentityServer4.Extensions;
using IdentityServer4.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer4.Endpoints.Results
{
    /// <summary>
    /// Result for login page
    /// </summary>
    /// <seealso cref="IdentityServer4.Hosting.IEndpointResult" />
    public class LoginPageResult : IEndpointResult
    {
        private readonly ValidatedAuthorizeRequest _request;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginPageResult"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="System.ArgumentNullException">request</exception>
        public LoginPageResult(ValidatedAuthorizeRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        internal LoginPageResult(
            ValidatedAuthorizeRequest request,
            IdentityServerOptions options) 
            : this(request)
        {
            _options = options;
        }

        private IdentityServerOptions _options;

        private void Init(HttpContext context)
        {
            _options = _options ?? context.RequestServices.GetRequiredService<IdentityServerOptions>();
        }

        /// <summary>
        /// Executes the result.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns></returns>
        public Task ExecuteAsync(HttpContext context)
        {
            Init(context);

            var returnUrl = context.GetIdentityServerBasePath().EnsureTrailingSlash() + Constants.ProtocolRoutePaths.AuthorizeCallback;
            returnUrl = returnUrl.AddQueryString(_request.Raw.ToQueryString());

            var loginUrl = _options.UserInteraction.LoginUrl;
            if (!loginUrl.IsLocalUrl())
            {
                // this converts the relative redirect path to an absolute one if we're 
                // redirecting to a different server
                returnUrl = context.GetIdentityServerHost().EnsureTrailingSlash() + returnUrl.RemoveLeadingSlash();
            }

            var url = loginUrl.AddQueryString(_options.UserInteraction.LoginReturnUrlParameter, returnUrl);
            context.Response.RedirectToAbsoluteUrl(url);

            return Task.CompletedTask;
        }
    }
}