// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Newtonsoft.Json;

namespace IdentityServer4.Stores.Serialization
{
    /// <summary>
    /// JSON-based persisted grant serializer
    /// </summary>
    /// <seealso cref="IdentityServer4.Stores.Serialization.IPersistentGrantSerializer" />
    public class PersistentGrantSerializer : IPersistentGrantSerializer
    {
        private readonly JsonSerializerSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentGrantSerializer"/> class.
        /// </summary>
        public PersistentGrantSerializer()
        {
            _settings = new JsonSerializerSettings();
            _settings.ContractResolver = new CustomContractResolver();
            _settings.Converters.Add(new ClaimConverter());
            _settings.Converters.Add(new ClaimsPrincipalConverter());
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value, _settings);
        }

        /// <summary>
        /// Deserializes the specified string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }
    }
}