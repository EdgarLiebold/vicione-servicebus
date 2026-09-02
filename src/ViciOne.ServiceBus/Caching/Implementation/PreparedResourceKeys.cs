#nullable enable
namespace ViciOne.ServiceBus.Caching.Implementation
{
    using System;
    using System.Collections.Generic;


    internal readonly struct PreparedResourceKeys<TValue>
        where TValue : class
    {
        public PreparedResourceKeys(long indexVersion, Dictionary<ResourceCacheIndexBase<TValue>, object> keys)
        {
            IndexVersion = indexVersion;
            Keys = keys;
        }

        public long IndexVersion { get; }
        public Dictionary<ResourceCacheIndexBase<TValue>, object> Keys { get; }

        public object GetKey(ResourceCacheIndexBase<TValue> index)
        {
            return Keys.TryGetValue(index, out var key)
                ? key
                : throw new InvalidOperationException($"Prepared key for index '{index.Name}' was not found.");
        }
    }
}
