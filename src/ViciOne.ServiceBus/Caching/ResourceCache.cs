#nullable enable
namespace ViciOne.ServiceBus.Caching
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Implementation;


    /// <summary>
    /// Owns the lifetime of a bounded set of asynchronous resources and any number of strongly typed indices over them.
    /// All cache and index mutations are committed atomically under one short critical section. Resource creation,
    /// disposal, key projection and observer callbacks are never executed while that critical section is held.
    /// </summary>
    public sealed class ResourceCache<TValue> :
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
        readonly AsyncLocal<int> _observerDispatchDepth;
        readonly HashSet<PendingResourceCreation<TValue>> _pendingCreations;
        readonly object _sync;
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
        Task _cleanupTask = Task.CompletedTask;
        bool _cleanupRunning;

        public ResourceCache(ResourceCacheOptions? options = null)
        {
            _options = options ?? new ResourceCacheOptions();
            _sync = new object();
            _entries = new Dictionary<long, ResourceCacheEntry<TValue>>();
            _indices = new Dictionary<string, ResourceCacheIndexBase<TValue>>(StringComparer.Ordinal);
            _observers = new List<IResourceCacheObserver<TValue>>();
            _observerDispatchGate = new SemaphoreSlim(1, 1);
            _observerDispatchDepth = new AsyncLocal<int>();
            _pendingCreations = new HashSet<PendingResourceCreation<TValue>>();
            _lifetimeCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_options.LifetimeCancellationToken);
            _lifetimeCancellationToken = _lifetimeCancellationSource.Token;
            _cleanupTimer = _options.TimeProvider.CreateTimer(TriggerCleanup, null, _options.CleanupInterval, _options.CleanupInterval);
        }

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
        public IResourceCacheIndex<TKey, TValue> AddIndex<TKey>(string name, Func<TValue, TKey> keySelector,
            ResourceFactory<TKey, TValue>? missingValueFactory = null, IEqualityComparer<TKey>? comparer = null)
            where TKey : notnull
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(keySelector);

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

                var prepared = new KeyValuePair<ResourceCacheEntry<TValue>, object>[snapshot.Length];
                var keys = new HashSet<TKey>(comparer);
                for (var i = 0; i < snapshot.Length; i++)
                {
                    var key = index.PrepareKey(snapshot[i].Value);
                    if (!keys.Add((TKey)key))
                        throw new InvalidOperationException($"Index '{name}' produced duplicate key '{key}'.");

                    prepared[i] = new KeyValuePair<ResourceCacheEntry<TValue>, object>(snapshot[i], key);
                }

                lock (_sync)
                {
                    ThrowIfUnavailable_NoLock();

                    if (_indices.ContainsKey(name))
                        throw new ArgumentException($"An index named '{name}' already exists.", nameof(name));

                    // Any resource mutation changes the projection input. Retry instead of publishing a stale index.
                    if (version != _indexVersion || snapshot.Length != _entries.Count || snapshot.Any(x => !x.Active || !_entries.ContainsKey(x.Id)))
                        continue;

                    foreach (var pair in prepared)
                    {
                        index.CommitKey(pair.Key, pair.Value);
                        pair.Key.Keys[index] = pair.Value;
                    }

                    _indices.Add(name, index);
                    _indexVersion++;
                    return index;
                }
            }
        }

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
        /// Adds a fully created resource. If capacity is full, the least recently relevant committed resource is evicted.
        /// When all capacity is currently occupied by in-flight creations, this call backpressures until one completes.
        /// </summary>
        public async ValueTask AddAsync(TValue value, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(value);
            using var operation = EnterOperation();

            var prepared = PrepareKeys(value);

            while (true)
            {
                List<ResourceCacheEntry<TValue>> removed;
                Task? waitForPending = null;
                ResourceCacheEntry<TValue>? committed = null;
                long now = _options.TimeProvider.GetTimestamp();

                lock (_sync)
                {
                    ThrowIfDisposed_NoLock();
                    removed = CollectExpired_NoLock(now);

                    if (prepared.IndexVersion != _indexVersion)
                    {
                        committed = null;
                    }
                    else
                    {
                        EnsureKeysAvailable_NoLock(prepared);

                        if (_entries.Count + _pendingCreations.Count < _options.Capacity)
                            committed = CommitValue_NoLock(value, prepared, now);
                        else if (TryEvictCapacityCandidate_NoLock(removed))
                            committed = CommitValue_NoLock(value, prepared, now);
                        else
                            waitForPending = GetPendingCompletion_NoLock();
                    }
                }

                await ReleaseEntriesAsync(removed, true, cancellationToken).ConfigureAwait(false);

                if (committed is not null)
                {
                    AttachUsage(committed);
                    await NotifyAddedAsync(committed.Value, cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (prepared.IndexVersion != ReadIndexVersion())
                {
                    prepared = PrepareKeys(value);
                    continue;
                }

                if (waitForPending is null)
                    continue;

                await waitForPending.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public ValueTask<IReadOnlyList<TValue>> GetValuesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_sync)
            {
                ThrowIfUnavailable_NoLock();
                IReadOnlyList<TValue> values = _entries.Values.Select(x => x.Value).ToArray();
                return ValueTask.FromResult(values);
            }
        }

        public ConnectHandle Connect(IResourceCacheObserver<TValue> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);

            lock (_sync)
            {
                ThrowIfUnavailable_NoLock();
                _observers.Add(observer);
            }

            return new ObserverConnectHandle(this, observer);
        }

        public async ValueTask CleanupExpiredAsync(CancellationToken cancellationToken = default)
        {
            using var operation = EnterOperation();
            List<ResourceCacheEntry<TValue>> removed;
            long now = _options.TimeProvider.GetTimestamp();
            lock (_sync)
            {
                ThrowIfDisposed_NoLock();
                removed = CollectExpired_NoLock(now);
            }

            await ReleaseEntriesAsync(removed, true, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
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
                    pending.Index.RemovePending(pending.RequestedKey, pending);
                    pending.Completion.TrySetException(new OperationCanceledException("The cache was cleared while the resource was being created."));
                }

                _indexVersion++;
            }

            foreach (var pending in invalidated)
                CancelSafely(pending.CreationCancellationSource, "Resource creation cancellation faulted during cache clear");

            await ReleaseEntriesAsync(removed, false, cancellationToken).ConfigureAwait(false);

            Task[] ownership = invalidated.Select(x => x.OwnershipReleased.Task).ToArray();
            if (ownership.Length > 0)
                await Task.WhenAll(ownership).WaitAsync(cancellationToken).ConfigureAwait(false);

            await NotifyClearedAsync(cancellationToken).ConfigureAwait(false);
        }

        public ValueTask DisposeAsync()
        {
            TaskCompletionSource completion;
            bool start;

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

                start = true;
            }

            CancelSafely(_lifetimeCancellationSource, "Resource creation cancellation faulted during cache disposal");

            if (start)
                _ = CompleteDisposeAsync(completion);

            return new ValueTask(completion.Task);
        }

        async Task CompleteDisposeAsync(TaskCompletionSource completion)
        {
            try
            {
                _cleanupTimer.Dispose();

                Task operationsDrained;
                Task cleanupTask;
                lock (_sync)
                {
                    operationsDrained = _operationsDrained?.Task ?? Task.CompletedTask;
                    cleanupTask = _cleanupTask;
                }

                await Task.WhenAll(operationsDrained, cleanupTask).ConfigureAwait(false);

                ResourceCacheEntry<TValue>[] removed;
                PendingResourceCreation<TValue>[] pending;
                lock (_sync)
                {
                    _disposed = true;

                    removed = _entries.Values.ToArray();
                    foreach (var entry in removed)
                        RemoveEntry_NoLock(entry, false);

                    pending = _pendingCreations.ToArray();
                    foreach (var creation in pending)
                    {
                        creation.Invalidated = true;
                        creation.Index.RemovePending(creation.RequestedKey, creation);
                        creation.Completion.TrySetCanceled(_lifetimeCancellationToken);
                    }

                    _observers.Clear();
                    _indexVersion++;
                }

                await ReleaseEntriesAsync(removed, false, CancellationToken.None).ConfigureAwait(false);

                Task[] ownership = pending.Select(x => x.OwnershipReleased.Task).ToArray();
                if (ownership.Length > 0)
                    await Task.WhenAll(ownership).ConfigureAwait(false);

                _observerDispatchGate.Dispose();
                _lifetimeCancellationSource.Dispose();
                completion.TrySetResult();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }

        internal async ValueTask<TValue> GetAsync<TKey>(ResourceCacheIndex<TKey, TValue> index, TKey key, CancellationToken cancellationToken)
            where TKey : notnull
        {
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

            await ReleaseEntriesAsync(removed, true, cancellationToken).ConfigureAwait(false);

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
                    else if (_entries.Count + _pendingCreations.Count < _options.Capacity || TryEvictCapacityCandidate_NoLock(removed))
                    {
                        _misses++;
                        pending = new PendingResourceCreation<TValue>(index, key!, _lifetimeCancellationSource.Token);
                        index.AddPending(key, pending);
                        _pendingCreations.Add(pending);
                        _activeOperations++;
                        creationOperation = new OperationLease(this);
                        startCreation = true;
                    }
                    else
                    {
                        // Every capacity slot is currently an in-flight creation. Do not exceed the hard bound;
                        // wait for any owner to finish and then retry the lookup/reservation atomically.
                        waitForPendingCapacity = GetPendingCompletion_NoLock();
                    }
                }

                await ReleaseEntriesAsync(removed, true, cancellationToken).ConfigureAwait(false);

                if (entry is not null)
                    return entry.Value;

                if (missingWithoutFactory)
                    throw new KeyNotFoundException($"Key not found: {key}");

                if (startCreation)
                {
                    pending!.Runner = CompleteCreationAsync(index, key, pending, factory!, creationOperation!);
                    return await pending.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
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

        internal async ValueTask<bool> RemoveAsync<TKey>(ResourceCacheIndex<TKey, TValue> index, TKey key, CancellationToken cancellationToken)
            where TKey : notnull
        {
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

            await ReleaseEntriesAsync(removed, true, cancellationToken).ConfigureAwait(false);
            return requested is not null;
        }

        async Task CompleteCreationAsync<TKey>(ResourceCacheIndex<TKey, TValue> index, TKey requestedKey, PendingResourceCreation<TValue> pending,
            ResourceFactory<TKey, TValue> factory, OperationLease operation)
            where TKey : notnull
        {
            using var ownedOperation = operation;
            TValue? value = null;
            ResourceCacheEntry<TValue>? committed = null;

            try
            {
                value = await factory(requestedKey, pending.CreationCancellationSource.Token).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("The resource factory returned null.");

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
                LogContext.Warning?.Log(exception, "Resource cache timed cleanup faulted");
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

            // Capacity is a hard memory/resource bound. MinAge is intentionally ignored under pressure.
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

        Task GetPendingCompletion_NoLock()
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
            if (entry.Value is not IResourceUsageSource source)
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
                LogContext.Warning?.Log(exception, "Resource cache could not subscribe to usage notifications");
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
                LogContext.Warning?.Log(exception, "Resource cache could not detach a concurrently removed usage notification");
            }
        }

        static void CancelSafely(CancellationTokenSource source, string faultMessage)
        {
            try
            {
                source.Cancel();
            }
            catch (Exception exception)
            {
                LogContext.Warning?.Log(exception, faultMessage);
            }
        }

        void CompletePending_NoLock(PendingResourceCreation<TValue> pending)
        {
            pending.Index.RemovePending(pending.RequestedKey, pending);
            _pendingCreations.Remove(pending);
            pending.OwnershipReleased.TrySetResult();
            pending.CreationCancellationSource.Dispose();
        }

        void CompleteInvalidatedPending(PendingResourceCreation<TValue> pending)
        {
            lock (_sync)
            {
                pending.Index.RemovePending(pending.RequestedKey, pending);
                _pendingCreations.Remove(pending);
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
                _creationFaults++;
                pending.Completion.TrySetException(exception);
                pending.OwnershipReleased.TrySetResult();
                pending.CreationCancellationSource.Dispose();
            }
        }

        async ValueTask ReleaseEntriesAsync(IEnumerable<ResourceCacheEntry<TValue>> entries, bool notifyRemoved, CancellationToken cancellationToken)
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
                    LogContext.Warning?.Log(exception, "Cached resource disposal faulted");
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
                LogContext.Warning?.Log(exception, "Uncommitted cached resource disposal faulted");
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
                LogContext.Warning?.Log(exception, "Resource cache could not detach usage notifications");
            }
            finally
            {
                entry.UsageSource = null;
                entry.UsageHandler = null;
            }
        }

        ValueTask NotifyAddedAsync(TValue value, CancellationToken cancellationToken)
        {
            return DispatchObserversAsync(observer => observer.ResourceAddedAsync(value, cancellationToken),
                "Resource cache observer faulted after resource add");
        }

        ValueTask NotifyRemovedAsync(TValue value, CancellationToken cancellationToken)
        {
            return DispatchObserversAsync(observer => observer.ResourceRemovedAsync(value, cancellationToken),
                "Resource cache observer faulted after resource removal");
        }

        ValueTask NotifyClearedAsync(CancellationToken cancellationToken)
        {
            return DispatchObserversAsync(observer => observer.CacheClearedAsync(cancellationToken),
                "Resource cache observer faulted after cache clear");
        }

        async ValueTask DispatchObserversAsync(Func<IResourceCacheObserver<TValue>, ValueTask> callback, string faultMessage)
        {
            // Observer callbacks are deliberately serialized and directly backpressure the committing caller.
            // Re-entry into this cache is rejected by EnterOperation before any mutation, avoiding both deadlocks
            // and an unbounded notification queue. Observer failures remain observational and never roll back a commit.
            await _observerDispatchGate.WaitAsync().ConfigureAwait(false);
            _observerDispatchDepth.Value++;
            try
            {
                foreach (var observer in SnapshotObservers())
                {
                    try
                    {
                        await callback(observer).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        LogContext.Warning?.Log(exception, faultMessage);
                    }
                }
            }
            finally
            {
                _observerDispatchDepth.Value--;
                _observerDispatchGate.Release();
            }
        }

        IResourceCacheObserver<TValue>[] SnapshotObservers()
        {
            lock (_sync)
                return _observers.ToArray();
        }

        void Disconnect(IResourceCacheObserver<TValue> observer)
        {
            lock (_sync)
                _observers.Remove(observer);
        }

        OperationLease EnterOperation()
        {
            if (_observerDispatchDepth.Value > 0)
                throw new InvalidOperationException("Resource cache observer callbacks must not re-enter the same cache.");

            lock (_sync)
            {
                ThrowIfUnavailable_NoLock();
                _activeOperations++;
                return new OperationLease(this);
            }
        }

        void ExitOperation()
        {
            TaskCompletionSource? drained = null;
            lock (_sync)
            {
                if (_activeOperations <= 0)
                    throw new InvalidOperationException("Resource cache operation accounting underflow.");

                _activeOperations--;
                if (_stopping && _activeOperations == 0)
                {
                    drained = _operationsDrained;
                    _operationsDrained = null;
                }
            }

            drained?.TrySetResult();
        }

        void ThrowIfUnavailable_NoLock()
        {
            ObjectDisposedException.ThrowIf(_stopping || _disposed, this);
        }

        void ThrowIfDisposed_NoLock()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        sealed class OperationLease :
            IDisposable
        {
            ResourceCache<TValue>? _owner;

            public OperationLease(ResourceCache<TValue> owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _owner, null)?.ExitOperation();
            }
        }

        sealed class ObserverConnectHandle :
            ConnectHandle
        {
            ResourceCache<TValue>? _cache;
            IResourceCacheObserver<TValue>? _observer;

            public ObserverConnectHandle(ResourceCache<TValue> cache, IResourceCacheObserver<TValue> observer)
            {
                _cache = cache;
                _observer = observer;
            }

            public void Disconnect()
            {
                var cache = Interlocked.Exchange(ref _cache, null);
                var observer = Interlocked.Exchange(ref _observer, null);

                if (cache is not null && observer is not null)
                    cache.Disconnect(observer);
            }

            public void Dispose()
            {
                Disconnect();
            }
        }
    }
}
