using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching.Implementation;

namespace ViciOne.ServiceBus.Caching;

public sealed partial class ResourceCache<TValue>
    where TValue : class
{
    void TriggerCleanup(object? _)
    {
        List<ResourceCacheEntry<TValue>> removed;
        TaskCompletionSource completion;
        long now = _options.TimeProvider.GetTimestamp();

        lock (_sync)
        {
            if (_stopping || _disposed || _cleanupRunning)
                return;

            removed = CollectExpired_NoLock(now);
            if (removed.Count == 0)
                return;

            _cleanupRunning = true;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _cleanupTask = completion.Task;
        }

        _ = CompleteTimedCleanupAsync(removed, completion);
    }

    async Task CompleteTimedCleanupAsync(List<ResourceCacheEntry<TValue>> removed, TaskCompletionSource completion)
    {
        try
        {
            await ReleaseEntriesAsync(removed, true, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogWarningSafely(exception, "Resource cache timed cleanup faulted");
        }
        finally
        {
            lock (_sync)
                _cleanupRunning = false;

            completion.TrySetResult();
        }
    }

    PreparedResourceKeys<TValue> PrepareKeys(TValue value)
    {
        while (true)
        {
            ResourceCacheIndexBase<TValue>[] indices;
            long version;

            lock (_sync)
            {
                ThrowIfDisposed_NoLock();
                indices = _indices.Values.ToArray();
                version = _indexVersion;
            }

            var keys = new Dictionary<ResourceCacheIndexBase<TValue>, object>(indices.Length);
            foreach (var index in indices)
                keys.Add(index, index.PrepareKey(value));

            lock (_sync)
            {
                ThrowIfDisposed_NoLock();
                if (version == _indexVersion)
                    return new PreparedResourceKeys<TValue>(version, keys);
            }
        }
    }

    long ReadIndexVersion()
    {
        lock (_sync)
            return _indexVersion;
    }

    ResourceCacheEntry<TValue> CommitValue_NoLock(TValue value, PreparedResourceKeys<TValue> prepared, long timestamp)
    {
        var entry = new ResourceCacheEntry<TValue>(++_nextEntryId, value, timestamp);

        foreach (var pair in prepared.Keys)
        {
            pair.Key.CommitKey(entry, pair.Value);
            entry.Keys.Add(pair.Key, pair.Value);
        }

        _entries.Add(entry.Id, entry);
        _totalCreated++;
        _indexVersion++;
        return entry;
    }

    void EnsureKeysAvailable_NoLock(PreparedResourceKeys<TValue> prepared)
    {
        foreach (var pair in prepared.Keys)
        {
            if (pair.Key.TryGetEntry(pair.Value, out var existing) && existing.Active)
                throw new InvalidOperationException($"Index '{pair.Key.Name}' already contains key '{pair.Value}'.");
        }
    }

    List<ResourceCacheEntry<TValue>> CollectExpired_NoLock(long now)
    {
        var removed = new List<ResourceCacheEntry<TValue>>();

        foreach (var entry in _entries.Values.ToArray())
        {
            if (!IsExpired_NoLock(entry, now))
                continue;

            RemoveEntry_NoLock(entry, true);
            removed.Add(entry);
        }

        return removed;
    }

    bool IsExpired_NoLock(ResourceCacheEntry<TValue> entry, long now)
    {
        if (_options.TimeProvider.GetElapsedTime(entry.CreatedTimestamp, now) < _options.MinAge)
            return false;

        var reference = _options.ExpirationMode == ResourceCacheExpirationMode.Absolute
            ? entry.CreatedTimestamp
            : entry.LastUsedTimestamp;

        return _options.TimeProvider.GetElapsedTime(reference, now) > _options.MaxAge;
    }

    bool TryEvictCapacityCandidate_NoLock(List<ResourceCacheEntry<TValue>> removed)
    {
        ResourceCacheEntry<TValue>? candidate = null;
        foreach (var entry in _entries.Values)
        {
            if (candidate is null || GetEvictionTimestamp(entry) < GetEvictionTimestamp(candidate))
                candidate = entry;
        }

        if (candidate is null)
            return false;

        // Capacity is a hard resource bound, so minimum age does not prevent eviction under pressure.
        RemoveEntry_NoLock(candidate, true);
        removed.Add(candidate);
        return true;
    }

    long GetEvictionTimestamp(ResourceCacheEntry<TValue> entry)
    {
        return _options.ExpirationMode == ResourceCacheExpirationMode.Absolute
            ? entry.CreatedTimestamp
            : entry.LastUsedTimestamp;
    }

    Task GetPendingCompletion_NoLockAsync()
    {
        Task[] pending = _pendingCreations.Select(x => x.OwnershipReleased.Task).ToArray();
        if (pending.Length == 0)
            throw new InvalidOperationException("Cache capacity was exhausted without a committed resource or pending creation.");

        return Task.WhenAny(pending);
    }

    void RemoveEntry_NoLock(ResourceCacheEntry<TValue> entry, bool eviction)
    {
        if (!entry.Active || !_entries.Remove(entry.Id))
            return;

        entry.Active = false;
        foreach (var pair in entry.Keys)
            pair.Key.RemoveKey(pair.Value, entry);

        if (eviction)
            _evictions++;

        _indexVersion++;
    }

    static void Touch_NoLock(ResourceCacheEntry<TValue> entry, long timestamp)
    {
        if (entry.Active)
            entry.LastUsedTimestamp = timestamp;
    }

    void Touch(ResourceCacheEntry<TValue> entry)
    {
        long timestamp = _options.TimeProvider.GetTimestamp();
        lock (_sync)
            Touch_NoLock(entry, timestamp);
    }

    void AttachUsage(ResourceCacheEntry<TValue> entry)
    {
        if (_options.ExpirationMode != ResourceCacheExpirationMode.Sliding
            || entry.Value is not IResourceUsageSource source)
            return;

        void Used() => Touch(entry);

        bool detach;
        try
        {
            source.Used += Used;
            lock (_sync)
            {
                detach = !entry.Active;
                if (!detach)
                {
                    entry.UsageSource = source;
                    entry.UsageHandler = Used;
                }
            }
        }
        catch (Exception exception)
        {
            LogWarningSafely(exception, "Resource cache could not subscribe to usage notifications");
            return;
        }

        if (!detach)
            return;

        try
        {
            source.Used -= Used;
        }
        catch (Exception exception)
        {
            LogWarningSafely(exception, "Resource cache could not detach a concurrently removed usage notification");
        }
    }

    async ValueTask ReleaseEntriesAsync(IEnumerable<ResourceCacheEntry<TValue>> entries, bool notifyRemoved,
        CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            DetachUsage(entry);

            if (notifyRemoved)
                await NotifyRemovedAsync(entry.Value, cancellationToken).ConfigureAwait(false);

            try
            {
                await DisposeResourceAsync(entry.Value).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                LogWarningSafely(exception, "Cached resource disposal faulted");
            }
        }
    }

    static async ValueTask DisposeResourceAsync(TValue value)
    {
        switch (value)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    static async ValueTask DisposeUncommittedResourceAsync(TValue value)
    {
        try
        {
            await DisposeResourceAsync(value).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogWarningSafely(exception, "Uncommitted cached resource disposal faulted");
        }
    }

    static void DetachUsage(ResourceCacheEntry<TValue> entry)
    {
        if (entry.UsageSource is null || entry.UsageHandler is null)
            return;

        try
        {
            entry.UsageSource.Used -= entry.UsageHandler;
        }
        catch (Exception exception)
        {
            LogWarningSafely(exception, "Resource cache could not detach usage notifications");
        }
        finally
        {
            entry.UsageSource = null;
            entry.UsageHandler = null;
        }
    }
}
