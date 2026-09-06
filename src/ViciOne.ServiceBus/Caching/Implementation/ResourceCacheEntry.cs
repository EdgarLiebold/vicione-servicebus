using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Caching.Implementation;

internal sealed class ResourceCacheEntry<TValue>
    where TValue : class
{
    public ResourceCacheEntry(long id, TValue value, long timestamp)
    {
        Id = id;
        Value = value;
        CreatedTimestamp = timestamp;
        LastUsedTimestamp = timestamp;
        Active = true;
        Keys = new Dictionary<ResourceCacheIndexBase<TValue>, object>();
    }

    public long Id { get; }
    public TValue Value { get; }
    public long CreatedTimestamp { get; }
    public long LastUsedTimestamp { get; set; }
    public bool Active { get; set; }
    public Dictionary<ResourceCacheIndexBase<TValue>, object> Keys { get; }
    public IResourceUsageSource? UsageSource { get; set; }
    public Action? UsageHandler { get; set; }
}
