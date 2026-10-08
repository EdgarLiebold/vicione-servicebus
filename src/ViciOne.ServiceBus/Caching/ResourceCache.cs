using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Caching.Implementation;

namespace ViciOne.ServiceBus.Caching;

/// <summary>
/// Owns the lifetime of a bounded set of asynchronous resources and any number of strongly typed indices over them.
/// Resource state and its unique indexes share one synchronization boundary. Resource creation,
/// disposal, key projection and observer callbacks execute outside that boundary.
/// </summary>
/// <typeparam name="TValue">The cache-owned resource type.</typeparam>
public sealed partial class ResourceCache<TValue> :
    IAsyncDisposable
    where TValue : class
{
    readonly CancellationTokenSource _lifetimeCancellationSource;
    readonly CancellationToken _lifetimeCancellationToken;
    readonly ITimer _cleanupTimer;
    readonly Dictionary<long, ResourceCacheEntry<TValue>> _entries;
    readonly Dictionary<string, ResourceCacheIndexBase<TValue>> _indices;
    readonly ResourceCacheOptions _options;
    readonly List<IResourceCacheObserver<TValue>> _observers;
    readonly SemaphoreSlim _observerDispatchGate;
    readonly AsyncLocal<ObserverDispatchScope?> _observerDispatchScope;
    readonly HashSet<PendingResourceCreation<TValue>> _pendingCreations;
    readonly object _sync;
    TaskCompletionSource _capacityChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _disposed;
    bool _stopping;
    int _activeOperations;
    TaskCompletionSource? _operationsDrained;
    TaskCompletionSource? _disposeCompletion;
    long _evictions;
    long _hits;
    long _indexVersion;
    long _misses;
    long _nextEntryId;
    long _creationFaults;
    long _totalCreated;
    int _retiringEntries;
    Task _cleanupTask = Task.CompletedTask;
    bool _cleanupRunning;

    /// <summary>Initializes a resource cache with the supplied runtime policy.</summary>
    /// <param name="options">The cache capacity, expiration and lifetime policy.</param>
    public ResourceCache(ResourceCacheOptions? options = null)
    {
        _options = options ?? new ResourceCacheOptions();
        _sync = new object();
        _entries = new Dictionary<long, ResourceCacheEntry<TValue>>();
        _indices = new Dictionary<string, ResourceCacheIndexBase<TValue>>(StringComparer.Ordinal);
        _observers = new List<IResourceCacheObserver<TValue>>();
        _observerDispatchScope = new AsyncLocal<ObserverDispatchScope?>();
        _pendingCreations = new HashSet<PendingResourceCreation<TValue>>();
        _observerDispatchGate = new SemaphoreSlim(1, 1);

        CancellationTokenSource? lifetimeCancellationSource = null;
        try
        {
            lifetimeCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_options.LifetimeCancellationToken);
            _lifetimeCancellationSource = lifetimeCancellationSource;
            _lifetimeCancellationToken = lifetimeCancellationSource.Token;
            _cleanupTimer = _options.TimeProvider.CreateTimer(TriggerCleanup, null, _options.CleanupInterval, _options.CleanupInterval);
        }
        catch
        {
            lifetimeCancellationSource?.Dispose();
            _observerDispatchGate.Dispose();
            throw;
        }
    }

    /// <summary>Gets a point-in-time snapshot of cache statistics.</summary>
    public ResourceCacheStatistics Statistics
    {
        get
        {
            lock (_sync)
            {
                return new ResourceCacheStatistics(_entries.Count, _pendingCreations.Count, _totalCreated, _hits, _misses,
                    _creationFaults, _evictions);
            }
        }
    }

    /// <summary>
    /// Adds a strongly typed unique index. Existing resources are projected before the index is published.
    /// If cache state changes while keys are being projected, the projection is retried against a fresh snapshot.
    /// </summary>
    /// <typeparam name="TKey">The key used for lookup.</typeparam>
    /// <param name="name">The unique index name.</param>
    /// <param name="keySelector">The function that projects an index key from a resource.</param>
    /// <param name="missingValueFactory">The optional factory used when this index does not contain a requested key.</param>
    /// <param name="comparer">The optional equality comparer for projected keys.</param>
    /// <returns>The newly published resource index.</returns>
    public IResourceCacheIndex<TKey, TValue> AddIndex<TKey>(string name, Func<TValue, TKey> keySelector,
        ResourceFactory<TKey, TValue>? missingValueFactory = null, IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(keySelector);
        using var operation = EnterOperation();

        var index = new ResourceCacheIndex<TKey, TValue>(this, name, keySelector, missingValueFactory, comparer);

        while (true)
        {
            ResourceCacheEntry<TValue>[] snapshot;
            long version;

            lock (_sync)
            {
                ThrowIfUnavailable_NoLock();

                if (_indices.ContainsKey(name))
                    throw new ArgumentException($"An index named '{name}' already exists.", nameof(name));

                snapshot = _entries.Values.ToArray();
                version = _indexVersion;
            }

            index.ResetEntries();
            var prepared = new KeyValuePair<ResourceCacheEntry<TValue>, object>[snapshot.Length];
            var keys = new HashSet<TKey>(comparer);
            for (var i = 0; i < snapshot.Length; i++)
            {
                var key = index.PrepareKey(snapshot[i].Value);
                if (!keys.Add((TKey)key))
                    throw new InvalidOperationException($"Index '{name}' produced duplicate key '{key}'.");

                object slot = index.PrepareEntry(snapshot[i], key);
                index.PublishEntry(slot); // This index is still private and unpublished.
                prepared[i] = new KeyValuePair<ResourceCacheEntry<TValue>, object>(snapshot[i], slot);
            }

            lock (_sync)
            {
                ThrowIfUnavailable_NoLock();

                if (_indices.ContainsKey(name))
                    throw new ArgumentException($"An index named '{name}' already exists.", nameof(name));

                // Any resource change invalidates the projection input. Retry instead of publishing a stale index.
                if (version != _indexVersion || snapshot.Length != _entries.Count || snapshot.Any(x => !x.Active || !_entries.ContainsKey(x.Id)))
                    continue;

                _indices.EnsureCapacity(_indices.Count + 1);
                foreach (var pair in prepared)
                    pair.Key.Slots.EnsureCapacity(pair.Key.Slots.Count + 1);

                foreach (var pair in prepared)
                    pair.Key.Slots.Add(index, pair.Value);

                _indices.Add(name, index);
                _indexVersion++;
                return index;
            }
        }
    }

    /// <summary>Gets a published index by name and key type.</summary>
    /// <typeparam name="TKey">The key used for lookup.</typeparam>
    /// <param name="name">The unique index name.</param>
    /// <returns>The matching strongly typed resource index.</returns>
    public IResourceCacheIndex<TKey, TValue> GetIndex<TKey>(string name)
        where TKey : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_sync)
        {
            ThrowIfUnavailable_NoLock();

            if (!_indices.TryGetValue(name, out var index))
                throw new KeyNotFoundException($"Index '{name}' was not found.");

            if (index is ResourceCacheIndex<TKey, TValue> typedIndex)
                return typedIndex;

            throw new InvalidOperationException($"Index '{name}' uses key type '{index.KeyType}', not '{typeof(TKey)}'.");
        }
    }

    /// <summary>
    /// Adds a fully created resource. If capacity is full, the least recently relevant committed resource is evicted
    /// and released before the new resource is admitted. Pending keys and resources being released backpressure admission.
    /// </summary>
    /// <param name="value">The resource whose ownership is transferred to the cache after a successful commit.</param>
    /// <param name="cancellationToken">The token checked before admission and used while waiting for pending capacity.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask AddAsync(TValue value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        using var operation = EnterOperation();

        var prepared = PrepareKeys(value);

        while (true)
        {
            var removed = new List<ResourceCacheEntry<TValue>>();
            Task? waitForCapacity = null;
            ResourceCacheEntry<TValue>? committed = null;
            long now = _options.TimeProvider.GetTimestamp();

            try
            {
                lock (_sync)
                {
                    ThrowIfDisposed_NoLock();
                    CollectExpired_NoLock(now, removed);

                    if (prepared.IndexVersion != _indexVersion)
                    {
                        committed = null;
                    }
                    else
                    {
                        EnsureKeysAvailable_NoLock(prepared);

                        if (HasPendingKey_NoLock(prepared))
                            waitForCapacity = WaitForCapacityChange_NoLockAsync();
                        else if (_entries.Count + _pendingCreations.Count + _retiringEntries < _options.Capacity)
                            committed = CommitValue_NoLock(value, prepared, now);
                        else if (!TryEvictCapacityCandidate_NoLock(removed))
                            waitForCapacity = WaitForCapacityChange_NoLockAsync();
                    }
                }
            }
            finally
            {
                await ReleaseEntriesAsync(removed, true, _lifetimeCancellationToken).ConfigureAwait(false);
            }

            if (committed is not null)
            {
                AttachUsage(committed);
                await NotifyAddedAsync(committed.Value, _lifetimeCancellationToken).ConfigureAwait(false);
                return;
            }

            if (prepared.IndexVersion != ReadIndexVersion())
            {
                prepared = PrepareKeys(value);
                continue;
            }

            if (waitForCapacity is null)
                continue;

            await waitForCapacity.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Gets a point-in-time snapshot of all committed resources.</summary>
    /// <param name="cancellationToken">The token that cancels snapshot acquisition.</param>
    /// <returns>The committed resources in unspecified order.</returns>
    public IReadOnlyList<TValue> GetValues(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            ThrowIfUnavailable_NoLock();
            return _entries.Values.Select(x => x.Value).ToArray();
        }
    }

    /// <summary>Registers an observer for subsequent committed cache changes.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle Connect(IResourceCacheObserver<TValue> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ThrowIfObserverMutation();

        lock (_sync)
        {
            ThrowIfUnavailable_NoLock();
            _observers.Add(observer);
        }

        return new ObserverConnectHandle(this, observer);
    }

    /// <summary>Removes and releases every resource whose configured lifetime has expired.</summary>
    /// <param name="cancellationToken">The token checked before cleanup starts; committed resource release is not canceled by this token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var operation = EnterOperation();
        var removed = new List<ResourceCacheEntry<TValue>>();
        long now = _options.TimeProvider.GetTimestamp();
        try
        {
            lock (_sync)
            {
                ThrowIfDisposed_NoLock();
                CollectExpired_NoLock(now, removed);
            }
        }
        finally
        {
            await ReleaseEntriesAsync(removed, true, _lifetimeCancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Removes committed resources and invalidates pending creations, awaiting their ownership release.</summary>
    /// <param name="cancellationToken">The token checked before clearing starts; committed cleanup is not canceled by this token.</param>
    /// <returns>A value task that completes after resource release and clear notifications.</returns>
    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var operation = EnterOperation();
        ResourceCacheEntry<TValue>[] removed;
        PendingResourceCreation<TValue>[] invalidated;

        lock (_sync)
        {
            ThrowIfDisposed_NoLock();

            removed = _entries.Values.ToArray();
            foreach (var entry in removed)
                RemoveEntry_NoLock(entry, false);

            invalidated = _pendingCreations.ToArray();
            foreach (var pending in invalidated)
            {
                pending.Invalidated = true;
                pending.Index.RemovePending(pending);
                pending.Completion.TrySetException(new OperationCanceledException("The cache was cleared while the resource was being created."));
            }

            _indexVersion++;
        }

        foreach (var pending in invalidated)
            CancelSafely(pending.CreationCancellationSource, "Resource creation cancellation faulted during cache clear");

        await ReleaseEntriesAsync(removed, false, _lifetimeCancellationToken).ConfigureAwait(false);

        Task[] ownership = invalidated.Select(x => x.OwnershipReleased.Task).ToArray();
        if (ownership.Length > 0)
            await Task.WhenAll(ownership).ConfigureAwait(false);

        await NotifyClearedAsync(_lifetimeCancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stops admission, cancels cache-owned creation and awaits all operations, timed cleanup and resource release.</summary>
    /// <returns>A value task that observes the shared disposal outcome.</returns>
    public ValueTask DisposeAsync()
    {
        ThrowIfObserverMutation();
        TaskCompletionSource completion;

        lock (_sync)
        {
            if (_disposeCompletion is not null)
                return new ValueTask(_disposeCompletion.Task);

            _stopping = true;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposeCompletion = completion;

            if (_activeOperations == 0)
                _operationsDrained = null;
            else
                _operationsDrained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        CancelSafely(_lifetimeCancellationSource, "Resource creation cancellation faulted during cache disposal");

        _ = CompleteDisposeAsync(completion);

        return new ValueTask(completion.Task);
    }
}
