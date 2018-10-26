// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace IdentityServer4.Models
{
    /// <summary>
    /// Models the data necessary for end session to trigger single signout.
    /// </summary>
    public class EndSession
    {
        /// <summary>
        ///  The SubjectId of the user.
        /// </summary>
        public string SubjectId { get; set; }

        /// <summary>
        /// The session Id of the user's authentication session.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// The list of client Ids that the user has authenticated to.
        /// </summary>
        public IEnumerable<string> ClientIds { get; set; }
    }
}
