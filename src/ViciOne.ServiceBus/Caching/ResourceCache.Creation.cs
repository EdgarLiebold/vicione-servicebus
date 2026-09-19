using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching.Implementation;

namespace ViciOne.ServiceBus.Caching;

public sealed partial class ResourceCache<TValue>
    where TValue : class
{
    internal async ValueTask<TValue> GetAsync<TKey>(ResourceCacheIndex<TKey, TValue> index, TKey key, CancellationToken cancellationToken)
        where TKey : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var operation = EnterOperation();
        List<ResourceCacheEntry<TValue>> removed;
        ResourceCacheEntry<TValue>? entry;
        PendingResourceCreation<TValue>? pending;
        long now = _options.TimeProvider.GetTimestamp();

        lock (_sync)
        {
            ThrowIfDisposed_NoLock();
            removed = CollectExpired_NoLock(now);

            if (index.TryGetEntry(key, out entry))
            {
                _hits++;
                Touch_NoLock(entry, now);
                pending = null;
            }
            else if (index.TryGetPending(key, out pending))
            {
                _hits++;
                entry = null;
            }
            else
            {
                _misses++;
                entry = null;
                pending = null;
            }
        }

        await ReleaseEntriesAsync(removed, true, _lifetimeCancellationToken).ConfigureAwait(false);

        if (entry is not null)
            return entry.Value;
        if (pending is not null)
            return await pending.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        throw new KeyNotFoundException($"Key not found: {key}");
    }

    internal async ValueTask<TValue> GetOrAddAsync<TKey>(ResourceCacheIndex<TKey, TValue> index, TKey key,
        ResourceFactory<TKey, TValue>? factory, CancellationToken cancellationToken)
        where TKey : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var operation = EnterOperation();
        factory ??= index.MissingValueFactory;

        while (true)
        {
            List<ResourceCacheEntry<TValue>> removed;
            ResourceCacheEntry<TValue>? entry = null;
            PendingResourceCreation<TValue>? pending = null;
            Task? waitForPendingCapacity = null;
            OperationLease? creationOperation = null;
            bool missingWithoutFactory = false;
            bool startCreation = false;
            long now = _options.TimeProvider.GetTimestamp();

            lock (_sync)
            {
                ThrowIfDisposed_NoLock();
                removed = CollectExpired_NoLock(now);

                if (index.TryGetEntry(key, out entry))
                {
                    _hits++;
                    Touch_NoLock(entry, now);
                }
                else if (index.TryGetPending(key, out pending))
                {
                    _hits++;
                }
                else if (factory is null)
                {
                    _misses++;
                    missingWithoutFactory = true;
                }
                else if (_entries.Count + _pendingCreations.Count + _retiringEntries < _options.Capacity)
                {
                    _misses++;
                    pending = new PendingResourceCreation<TValue>(index, key!, _lifetimeCancellationSource.Token);
                    index.AddPending(key, pending);
                    _pendingCreations.Add(pending);
                    _activeOperations++;
                    creationOperation = new OperationLease(this);
                    startCreation = true;
                }
                else if (!TryEvictCapacityCandidate_NoLock(removed))
                {
                    // Pending creations and removed resources retain their slots until ownership is released.
                    waitForPendingCapacity = WaitForCapacityChange_NoLockAsync();
                }
            }

            await ReleaseEntriesAsync(removed, true, _lifetimeCancellationToken).ConfigureAwait(false);

            if (entry is not null)
                return entry.Value;

            if (missingWithoutFactory)
                throw new KeyNotFoundException($"Key not found: {key}");

            if (startCreation)
            {
                PendingResourceCreation<TValue> ownedCreation = pending!;
                _ = CompleteCreationAsync(index, key, ownedCreation, factory!, creationOperation!);
                return await ownedCreation.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            if (pending is not null)
                return await pending.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (waitForPendingCapacity is not null)
            {
                await waitForPendingCapacity.WaitAsync(cancellationToken).ConfigureAwait(false);
                continue;
            }
        }
    }

    internal async ValueTask<bool> RemoveAsync<TKey>(ResourceCacheIndex<TKey, TValue> index, TKey key,
        CancellationToken cancellationToken)
        where TKey : notnull
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var operation = EnterOperation();
        List<ResourceCacheEntry<TValue>> removed;
        ResourceCacheEntry<TValue>? requested = null;
        long now = _options.TimeProvider.GetTimestamp();

        lock (_sync)
        {
            ThrowIfDisposed_NoLock();
            removed = CollectExpired_NoLock(now);

            if (!index.TryGetPending(key, out _) && index.TryGetEntry(key, out requested))
                RemoveEntry_NoLock(requested, false);
        }

        if (requested is not null)
            removed.Add(requested);

        await ReleaseEntriesAsync(removed, true, _lifetimeCancellationToken).ConfigureAwait(false);
        return requested is not null;
    }

    async Task CompleteCreationAsync<TKey>(ResourceCacheIndex<TKey, TValue> index, TKey requestedKey,
        PendingResourceCreation<TValue> pending, ResourceFactory<TKey, TValue> factory, OperationLease operation)
        where TKey : notnull
    {
        using var ownedOperation = operation;
        TValue? value = null;
        ResourceCacheEntry<TValue>? committed = null;

        try
        {
            value = await factory(requestedKey, pending.CreationCancellationSource.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The resource factory returned null.");
            pending.CreationCancellationSource.Token.ThrowIfCancellationRequested();

            while (true)
            {
                lock (_sync)
                {
                    if (_disposed || pending.Invalidated)
                        break;
                }

                PreparedResourceKeys<TValue> prepared = PrepareKeys(value);
                long timestamp = _options.TimeProvider.GetTimestamp();
                bool retryProjection;

                lock (_sync)
                {
                    retryProjection = prepared.IndexVersion != _indexVersion;
                    if (retryProjection)
                        continue;

                    if (_disposed || pending.Invalidated)
                        break;

                    if (!index.PreparedKeyMatches(requestedKey, prepared.GetKey(index)))
                    {
                        throw new InvalidOperationException(
                            $"Factory for index '{index.Name}' key '{requestedKey}' created a resource whose projected key is '{prepared.GetKey(index)}'.");
                    }

                    EnsureKeysAvailable_NoLock(prepared);
                    committed = CommitValue_NoLock(value, prepared, timestamp);
                    CompletePending_NoLock(pending);
                }

                break;
            }

            if (committed is not null)
            {
                AttachUsage(committed);
                await NotifyAddedAsync(committed.Value, _lifetimeCancellationToken).ConfigureAwait(false);
                pending.Completion.TrySetResult(value);
                return;
            }

            await DisposeUncommittedResourceAsync(value).ConfigureAwait(false);
            CompleteInvalidatedPending(pending);
        }
        catch (OperationCanceledException) when (_lifetimeCancellationSource.IsCancellationRequested || pending.Invalidated)
        {
            if (value is not null && committed is null)
                await DisposeUncommittedResourceAsync(value).ConfigureAwait(false);

            CompleteCanceledPending(pending);
        }
        catch (Exception exception)
        {
            if (value is not null && committed is null)
                await DisposeUncommittedResourceAsync(value).ConfigureAwait(false);

            CompleteFaultedPending(pending, exception);
        }
    }

    void CompletePending_NoLock(PendingResourceCreation<TValue> pending)
    {
        pending.Index.RemovePending(pending.RequestedKey, pending);
        _pendingCreations.Remove(pending);
        SignalCapacityChanged_NoLock();
        pending.OwnershipReleased.TrySetResult();
        pending.CreationCancellationSource.Dispose();
    }

    void CompleteInvalidatedPending(PendingResourceCreation<TValue> pending)
    {
        lock (_sync)
        {
            pending.Index.RemovePending(pending.RequestedKey, pending);
            _pendingCreations.Remove(pending);
            SignalCapacityChanged_NoLock();
            pending.Completion.TrySetException(new OperationCanceledException("Resource creation was invalidated by the cache owner."));
            pending.OwnershipReleased.TrySetResult();
            pending.CreationCancellationSource.Dispose();
        }
    }

    void CompleteCanceledPending(PendingResourceCreation<TValue> pending)
    {
        lock (_sync)
        {
            pending.Index.RemovePending(pending.RequestedKey, pending);
            _pendingCreations.Remove(pending);
            SignalCapacityChanged_NoLock();
            pending.Completion.TrySetCanceled(_lifetimeCancellationToken);
            pending.OwnershipReleased.TrySetResult();
            pending.CreationCancellationSource.Dispose();
        }
    }

    void CompleteFaultedPending(PendingResourceCreation<TValue> pending, Exception exception)
    {
        lock (_sync)
        {
            pending.Index.RemovePending(pending.RequestedKey, pending);
            _pendingCreations.Remove(pending);
            SignalCapacityChanged_NoLock();
            _creationFaults++;
            pending.Completion.TrySetException(exception);
            pending.OwnershipReleased.TrySetResult();
            pending.CreationCancellationSource.Dispose();
        }
    }
}
