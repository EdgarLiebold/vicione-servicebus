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
    readonly Dictionary<TKey, ResourceCacheEntry<TValue>> _entries;
    readonly Func<TValue, TKey> _keySelector;
    readonly Dictionary<TKey, PendingResourceCreation<TValue>> _pending;
    readonly ResourceCache<TValue> _owner;

    public ResourceCacheIndex(ResourceCache<TValue> owner, string name, Func<TValue, TKey> keySelector,
        ResourceFactory<TKey, TValue>? missingValueFactory, IEqualityComparer<TKey>? comparer)
        : base(name)
    {
        _owner = owner;
        _keySelector = keySelector;
        MissingValueFactory = missingValueFactory;
        _comparer = comparer ?? EqualityComparer<TKey>.Default;
        _entries = new Dictionary<TKey, ResourceCacheEntry<TValue>>(_comparer);
        _pending = new Dictionary<TKey, PendingResourceCreation<TValue>>(_comparer);
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

    public void AddPending(TKey key, PendingResourceCreation<TValue> pending)
    {
        _pending.Add(key, pending);
    }

    public override void CommitKey(ResourceCacheEntry<TValue> entry, object key)
    {
        _entries.Add((TKey)key, entry);
    }

    public override void RemoveKey(object key, ResourceCacheEntry<TValue> entry)
    {
        var typedKey = (TKey)key;
        if (_entries.TryGetValue(typedKey, out var current) && ReferenceEquals(current, entry))
            _entries.Remove(typedKey);
    }

    public override bool PreparedKeyMatches(object requestedKey, object preparedKey)
    {
        return _comparer.Equals((TKey)requestedKey, (TKey)preparedKey);
    }

    public override void RemovePending(object key, PendingResourceCreation<TValue> pending)
    {
        var typedKey = (TKey)key;
        if (_pending.TryGetValue(typedKey, out var current) && ReferenceEquals(current, pending))
            _pending.Remove(typedKey);
    }

    static void ValidateKey(TKey key)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));
    }
}
