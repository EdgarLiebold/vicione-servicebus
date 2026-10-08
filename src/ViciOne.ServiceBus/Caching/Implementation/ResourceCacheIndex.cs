using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching.Implementation;

internal sealed class ResourceCacheIndex<TKey, TValue> :
    ResourceCacheIndexBase<TValue>,
    IResourceCacheIndex<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    readonly IEqualityComparer<TKey> _comparer;
    readonly PreparedResourceMap<TKey, ResourceCacheEntry<TValue>> _entries;
    readonly Func<TValue, TKey> _keySelector;
    readonly PreparedResourceMap<TKey, PendingResourceCreation<TValue>> _pending;
    readonly ResourceCache<TValue> _owner;

    public ResourceCacheIndex(ResourceCache<TValue> owner, string name, Func<TValue, TKey> keySelector,
        ResourceFactory<TKey, TValue>? missingValueFactory, IEqualityComparer<TKey>? comparer)
        : base(name)
    {
        _owner = owner;
        _keySelector = keySelector;
        MissingValueFactory = missingValueFactory;
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _entries = new PreparedResourceMap<TKey, ResourceCacheEntry<TValue>>(_comparer);
        _pending = new PreparedResourceMap<TKey, PendingResourceCreation<TValue>>(_comparer);
    }

    public override Type KeyType => typeof(TKey);
    public ResourceFactory<TKey, TValue>? MissingValueFactory { get; }

    public ValueTask<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return _owner.GetAsync(this, key, cancellationToken);
    }

    public ValueTask<TValue> GetOrAddAsync(TKey key, ResourceFactory<TKey, TValue>? factory = null,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return _owner.GetOrAddAsync(this, key, factory, cancellationToken);
    }

    public ValueTask<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return _owner.RemoveAsync(this, key, cancellationToken);
    }

    public override object PrepareKey(TValue value)
    {
        var key = _keySelector(value);
        return key is null
            ? throw new InvalidOperationException($"Index '{Name}' produced a null key.")
            : key;
    }

    public bool TryGetEntry(TKey key, [NotNullWhen(true)] out ResourceCacheEntry<TValue>? entry)
    {
        return _entries.TryGetValue(key, out entry);
    }

    public override bool TryGetEntry(object key, [NotNullWhen(true)] out ResourceCacheEntry<TValue>? entry)
    {
        return _entries.TryGetValue((TKey)key, out entry);
    }

    public bool TryGetPending(TKey key, [NotNullWhen(true)] out PendingResourceCreation<TValue>? pending)
    {
        return _pending.TryGetValue(key, out pending);
    }

    public override bool TryGetPending(object key, [NotNullWhen(true)] out PendingResourceCreation<TValue>? pending)
    {
        return _pending.TryGetValue((TKey)key, out pending);
    }

    public object PreparePending(TKey key, PendingResourceCreation<TValue> pending)
    {
        return _pending.Prepare(key, pending);
    }

    public void PublishPending(object slot, PendingResourceCreation<TValue> pending)
    {
        var prepared = (PreparedResourceMap<TKey, PendingResourceCreation<TValue>>.Slot)slot;
        _pending.Publish(prepared);
        pending.Slot = prepared;
    }

    public override object PrepareEntry(ResourceCacheEntry<TValue> entry, object key)
    {
        return _entries.Prepare((TKey)key, entry);
    }

    public override void PublishEntry(object slot)
    {
        _entries.Publish((PreparedResourceMap<TKey, ResourceCacheEntry<TValue>>.Slot)slot);
    }

    public override void RemoveEntrySlot(object slot)
    {
        _entries.Remove((PreparedResourceMap<TKey, ResourceCacheEntry<TValue>>.Slot)slot);
    }

    public void ResetEntries()
    {
        _entries.Clear();
    }

    public override bool PreparedKeyMatches(object requestedKey, object preparedKey)
    {
        return _comparer.Equals((TKey)requestedKey, (TKey)preparedKey);
    }

    public override void RemovePending(PendingResourceCreation<TValue> pending)
    {
        if (pending.Slot is null)
            return;

        _pending.Remove((PreparedResourceMap<TKey, PendingResourceCreation<TValue>>.Slot)pending.Slot);
        pending.Slot = null;
    }

    static void ValidateKey(TKey key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
    }
}
