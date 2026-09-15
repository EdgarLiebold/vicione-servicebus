using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Caching.Implementation;

/// <summary>Defines the untyped index operations used to project and update shared resource state.</summary>
internal abstract class ResourceCacheIndexBase<TValue>
    where TValue : class
{
    protected ResourceCacheIndexBase(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public abstract Type KeyType { get; }

    public abstract object PrepareKey(TValue value);
    public abstract bool TryGetEntry(object key, [NotNullWhen(true)] out ResourceCacheEntry<TValue>? entry);
    public abstract void CommitKey(ResourceCacheEntry<TValue> entry, object key);
    public abstract void RemoveKey(object key, ResourceCacheEntry<TValue> entry);
    public abstract bool PreparedKeyMatches(object requestedKey, object preparedKey);
    public abstract void RemovePending(object key, PendingResourceCreation<TValue> pending);
}
