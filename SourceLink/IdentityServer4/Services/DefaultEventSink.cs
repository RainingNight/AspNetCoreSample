// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Events;
using IdentityServer4.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace IdentityServer4.Services
{
    /// <summary>
    /// Default implementation of the event service. Write events raised to the log.
    /// </summary>
    public class DefaultEventSink : IEventSink
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEventSink"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public DefaultEventSink(ILogger<DefaultEventService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Raises the specified event.
        /// </summary>
        /// <param name="evt">The event.</param>
        /// <exception cref="System.ArgumentNullException">evt</exception>
        public virtual Task PersistAsync(Event evt)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            var json = LogSerializer.Serialize(evt);
            _logger.LogInformation(json);

            return Task.CompletedTask;
        }
    }
}