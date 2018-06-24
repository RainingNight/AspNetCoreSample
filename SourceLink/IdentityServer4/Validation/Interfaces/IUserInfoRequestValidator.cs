// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;

namespace IdentityServer4.Validation
{
    /// <summary>
    /// Validator for userinfo requests
    /// </summary>
    public interface IUserInfoRequestValidator
    {
        /// <summary>
        /// Validates a userinfo request.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        Task<UserInfoRequestValidationResult> ValidateRequestAsync(string accessToken);
    }
}
