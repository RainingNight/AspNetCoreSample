// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Validation;

#pragma warning disable 1591

namespace IdentityServer4.ResponseHandling
{
    public class AuthorizeResponse
    {
        public ValidatedAuthorizeRequest Request { get; set; }
        public string RedirectUri => Request?.RedirectUri;
        public string State => Request?.State;
        public string Scope => Request?.ValidatedScopes?.GrantedResources?.ToScopeNames()?.ToSpaceSeparatedString();

        public string IdentityToken { get; set; }
        public string AccessToken { get; set; }
        public int AccessTokenLifetime { get; set; }
        public string Code { get; set; }
        public string SessionState { get; set; }

        public string Error { get; set; }
        public string ErrorDescription { get; set; }
        public bool IsError => Error.IsPresent();
    }
}