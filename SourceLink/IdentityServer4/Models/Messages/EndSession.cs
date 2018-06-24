// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace IdentityServer4.Models
{
    /// <summary>
    /// Models the data necessary for end session to trigger single signout.
    /// </summary>
    internal class EndSession
    {
        public string SubjectId { get; set; }
        public string SessionId { get; set; }
        public IEnumerable<string> ClientIds { get; set; }
    }
}
