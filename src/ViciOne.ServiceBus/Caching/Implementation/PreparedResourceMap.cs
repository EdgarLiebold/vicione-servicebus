using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Caching.Implementation;

/// <summary>Separates fallible user-key comparison from callback-free publication and identity removal.</summary>
/// <remarks>Published maps are operated under the cache monitor; an unpublished index is privately owned.
/// Keys must retain ordinary stable hash/equality semantics.</remarks>
internal sealed class PreparedResourceMap<TKey, TValue>
    where TKey : notnull
    where TValue : class
{
    readonly Dictionary<int, Bucket> _buckets = new();
    readonly IEqualityComparer<TKey> _comparer;
    bool _initialized;

    public PreparedResourceMap(IEqualityComparer<TKey> comparer)
    {
        _comparer = comparer;
    }

    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        if (!_initialized)
        {
            value = null;
            return false;
        }

        int hash = _comparer.GetHashCode(key);
        if (_buckets.TryGetValue(hash, out Bucket? bucket))
        {
            for (Slot? slot = bucket.First; slot is not null; slot = slot.Next)
            {
                if (!_comparer.Equals(slot.Key, key))
                    continue;

                value = slot.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public Slot Prepare(TKey key, TValue value)
    {
        if (!_initialized)
        {
            // Match Dictionary's lazy lookup boundary: attempted insertion initializes
            // backing state before fallible user hashing, even if that attempt fails.
            _buckets.EnsureCapacity(1);
            _initialized = true;
        }

        int hash = _comparer.GetHashCode(key);
        if (_buckets.TryGetValue(hash, out Bucket? bucket))
        {
            for (Slot? existing = bucket.First; existing is not null; existing = existing.Next)
                if (_comparer.Equals(existing.Key, key))
                    throw new InvalidOperationException("A resource key is already present.");
        }
        else
        {
            bucket = new Bucket(hash);
            // A rejected prepared slot publishes no bucket; backing capacity stays bounded
            // by the current live bucket count plus one, rather than failed-attempt history.
            _buckets.EnsureCapacity(_buckets.Count + 1);
        }

        return new Slot(this, bucket, key, value);
    }

    public void Publish(Slot slot)
    {
        ValidateOwner(slot);
        if (slot.Linked)
            throw new InvalidOperationException("A resource slot is already published.");

        Bucket bucket = slot.Bucket;
        if (_buckets.TryGetValue(bucket.Hash, out Bucket? current))
        {
            if (!ReferenceEquals(current, bucket))
                throw new InvalidOperationException("The prepared resource bucket is no longer current.");
        }
        else
            _buckets.Add(bucket.Hash, bucket);

        slot.Previous = bucket.Last;
        if (bucket.Last is null)
            bucket.First = slot;
        else
            bucket.Last.Next = slot;
        bucket.Last = slot;
        slot.Linked = true;
    }

    public void Remove(Slot slot)
    {
        ValidateOwner(slot);
        if (!slot.Linked)
            return;

        Bucket bucket = slot.Bucket;
        if (slot.Previous is null)
            bucket.First = slot.Next;
        else
            slot.Previous.Next = slot.Next;
        if (slot.Next is null)
            bucket.Last = slot.Previous;
        else
            slot.Next.Previous = slot.Previous;

        slot.Linked = false;
        slot.Previous = slot.Next = null;
        slot.Key = default!;
        slot.Value = default!;
        if (bucket.First is null)
            _buckets.Remove(bucket.Hash);
    }

    public void Clear()
    {
        foreach (Bucket bucket in _buckets.Values)
        {
            Slot? slot = bucket.First;
            while (slot is not null)
            {
                Slot? next = slot.Next;
                slot.Linked = false;
                slot.Previous = slot.Next = null;
                slot.Key = default!;
                slot.Value = default!;
                slot = next;
            }
            bucket.First = bucket.Last = null;
        }
        _buckets.Clear();
        // Clear is used only to rebuild an unpublished index against a fresh snapshot.
        _initialized = false;
    }

    void ValidateOwner(Slot slot)
    {
        if (!ReferenceEquals(slot.Owner, this))
            throw new InvalidOperationException("The resource slot belongs to a different map.");
    }

    internal sealed class Bucket(int hash)
    {
        public int Hash { get; } = hash;
        public Slot? First { get; set; }
        public Slot? Last { get; set; }
    }

    internal sealed class Slot(PreparedResourceMap<TKey, TValue> owner, Bucket bucket, TKey key, TValue value)
    {
        public PreparedResourceMap<TKey, TValue> Owner { get; } = owner;
        public Bucket Bucket { get; } = bucket;
        public TKey Key { get; set; } = key;
        public TValue Value { get; set; } = value;
        public Slot? Previous { get; set; }
        public Slot? Next { get; set; }
        public bool Linked { get; set; }
    }
}
