namespace ViciOne.ServiceBus.Caching.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Internals;


    public class NodeTracker<TValue> :
        INodeTracker<TValue>
        where TValue : class
    {
        const double MaxAgeUpperLimit = 24.0 * 60 * 60 * 1000;
        readonly int _bucketCount;
        readonly int _bucketSize;
        readonly object _lock = new object();
        readonly TimeSpan _maxAge;
        readonly TimeSpan _minAge;
        readonly CurrentTimeProvider _nowProvider;
        readonly CacheValueObservable<TValue> _observers;
        readonly Func<Action, bool> _tryScheduleCleanup;
        readonly TimeSpan _validityCheckInterval;
        BucketCollection<TValue> _buckets;
        DateTime _cacheResetTime;
        bool _cleanupRequested;
        DateTime _cleanupRequestedAt;
        bool _cleanupScheduled;
        Bucket<TValue> _currentBucket;
        int _currentBucketIndex;
        DateTime _nextValidityCheck;
        int _oldestBucketIndex;

        public NodeTracker(CacheSettings settings)
            : this(settings, TryScheduleOnThreadPool)
        {
        }

        internal NodeTracker(CacheSettings settings, Func<Action, bool> tryScheduleCleanup)
        {
            _tryScheduleCleanup = tryScheduleCleanup ?? throw new ArgumentNullException(nameof(tryScheduleCleanup));
            _nowProvider = settings.NowProvider;
            var now = _nowProvider();

            _observers = new CacheValueObservable<TValue>();

            var maxAgeInMilliseconds = Math.Min(settings.MaxAge.TotalMilliseconds, MaxAgeUpperLimit);

            _minAge = settings.MinAge;
            _maxAge = TimeSpan.FromMilliseconds(maxAgeInMilliseconds);

            _validityCheckInterval = TimeSpan.FromMilliseconds(maxAgeInMilliseconds / settings.TimeSlots);
            _cacheResetTime = now.Add(TimeSpan.FromMilliseconds(MaxAgeUpperLimit));

            _bucketSize = Math.Max(settings.Capacity / settings.BucketCount, 1);

            // enough buckets for all of the content within the time slices, plus a few spares
            _bucketCount = settings.TimeSlots * settings.BucketCount + 5;

            _buckets = new BucketCollection<TValue>(this, _bucketCount);

            Statistics = new CacheStatistics(settings.Capacity, _bucketCount, _bucketSize, _minAge, _maxAge, _validityCheckInterval);

            OpenBucket(0, now);
        }

        int OldestBucketIndex
        {
            get => _oldestBucketIndex;
            set
            {
                _oldestBucketIndex = value;
                Statistics.SetBucketIndices(_oldestBucketIndex, _currentBucketIndex);
            }
        }

        int CurrentBucketIndex
        {
            get => _currentBucketIndex;
            set
            {
                _currentBucketIndex = value;
                Statistics.SetBucketIndices(_oldestBucketIndex, _currentBucketIndex);
            }
        }

        bool AreLowOnBuckets => CurrentBucketIndex - OldestBucketIndex > _buckets.Count - 5;

        bool CanOpenBucketWithoutForcingEviction =>
            CurrentBucketIndex - OldestBucketIndex < _buckets.Count - 5;

        bool IsCurrentBucketOldest => OldestBucketIndex == CurrentBucketIndex;

        public CacheStatistics Statistics { get; }

        public void Add(INodeValueFactory<TValue> nodeValueFactory)
        {
            Task.Run(() => AddNode(nodeValueFactory));
        }

        public void Add(TValue value)
        {
            AddValue(value);
        }

        public void Remove(IBucketNode<TValue> node)
        {
            Task.Run(() => RemoveNode(node));
        }

        public IEnumerable<INode<TValue>> GetAll()
        {
            for (var bucketIndex = CurrentBucketIndex; bucketIndex >= OldestBucketIndex; --bucketIndex)
            {
                Bucket<TValue> bucket = _buckets[bucketIndex];

                // this is weird, but once a bucket starts being emptied, we want to stop walking the list since
                // it's a total race condition - so check if the bucket has a First, and if it doesn't, we're done
                // with this bucket. Prevents having to take a lock on it.
                for (IBucketNode<TValue> node = bucket.Head; node != null && bucket.Head != null; node = node.Next)
                {
                    if (node.IsValid)
                        yield return node;
                }
            }
        }

        public void Clear()
        {
            var now = _nowProvider();
            List<EvictedValue> evictedValues;
            lock (_lock)
                evictedValues = ResetCache(now);

            ScheduleResetRelease(evictedValues);

            // Observer callbacks may acquire index locks. They must never execute while the
            // tracker lock is held, otherwise a concurrent indexed read can invert the order.
            _observers.CacheCleared();
        }

        public void Rebucket(IBucketNode<TValue> node)
        {
            lock (_lock)
                node.AssignToBucket(_currentBucket);
        }

        public ConnectHandle Connect(ICacheValueObserver<TValue> observer)
        {
            return _observers.Connect(observer);
        }

        bool IsCleanupRequired(DateTime now)
        {
            return _currentBucket.Count > _bucketSize || now > _nextValidityCheck;
        }

        async Task AddNode(INodeValueFactory<TValue> nodeValueFactory)
        {
            try
            {
                var value = await nodeValueFactory.CreateValue().ConfigureAwait(false);

                Statistics.Miss();

                AddValue(value);
            }
            catch (Exception)
            {
                Statistics.CreateFaulted();
            }
        }

        void AddValue(TValue value)
        {
            var now = _nowProvider();
            var node = new BucketNode<TValue>(value);
            Action cleanup;
            var cacheReset = false;
            List<EvictedValue> resetValues = null;

            lock (_lock)
            {
                if (CurrentBucketIndex > 1000000000 || now >= _cacheResetTime)
                {
                    resetValues = ResetCache(now);
                    cacheReset = true;
                }

                _currentBucket.Push(node);

                Statistics.ValueAdded();

                cleanup = CheckCacheStatus(now);
            }

            if (resetValues is not null)
                ScheduleResetRelease(resetValues);

            ExceptionDispatchInfo observerFailure = null;
            try
            {
                if (cacheReset)
                    _observers.CacheCleared();
            }
            catch (Exception exception)
            {
                observerFailure = ExceptionDispatchInfo.Capture(exception);
            }

            try
            {
                _observers.ValueAdded(node, value);
            }
            catch (Exception exception)
            {
                observerFailure ??= ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                // Once the reservation is published, observer failures must not strand it.
                DispatchCleanup(cleanup);
            }

            observerFailure?.Throw();
        }

        async Task RemoveNode(IBucketNode<TValue> node)
        {
            try
            {
                var now = _nowProvider();
                TValue value;
                Action cleanup;
                lock (_lock)
                {
                    if (!node.TryEvict(out value))
                        return;

                    Statistics.ValueRemoved();
                    cleanup = CheckCacheStatus(now);
                }

                DetachUsageNotification(node, value);

                ExceptionDispatchInfo observerFailure = null;
                try
                {
                    _observers.ValueRemoved(node, value);
                }
                catch (Exception exception)
                {
                    observerFailure = ExceptionDispatchInfo.Capture(exception);
                }
                finally
                {
                    // Once the reservation is published, observer failures must not strand it.
                    DispatchCleanup(cleanup);
                }

                switch (value)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }

                observerFailure?.Throw();
            }
            catch
            {
                //
            }
        }

        void OpenBucket(int index, DateTime now)
        {
            var lockTaken = false;

            try
            {
                Monitor.Enter(_lock, ref lockTaken);

                if (_currentBucket != null)
                {
                    lock (_currentBucket)
                        _currentBucket.Stop(now);
                }

                CurrentBucketIndex = index;

                Bucket<TValue> openingBucket = _buckets[index];
                openingBucket.Start(now);

                _currentBucket = openingBucket;

                _nextValidityCheck = now.Add(_validityCheckInterval);
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(_lock);
            }
        }

        Action CheckCacheStatus(DateTime now)
        {
            lock (_lock)
            {
                if (!IsCleanupRequired(now))
                    return null;

                if (_cleanupScheduled)
                {
                    if (!_cleanupRequested || now > _cleanupRequestedAt)
                        _cleanupRequestedAt = now;

                    _cleanupRequested = true;

                    // Preserve size and time segmentation while the reserved cleanup waits, but
                    // never wrap onto a bucket that the cleanup has not reclaimed yet.
                    if (CanOpenBucketWithoutForcingEviction)
                        OpenBucket(++CurrentBucketIndex, now);

                    return null;
                }

                Volatile.Write(ref _cleanupScheduled, true);

                return () => Cleanup(now);
            }
        }

        void DispatchCleanup(Action cleanup)
        {
            if (cleanup == null)
                return;

            var invoked = 0;
            void RunOnce()
            {
                if (Interlocked.Exchange(ref invoked, 1) == 0)
                    cleanup();
            }

            var accepted = false;
            try
            {
                // Ownership is offered outside the tracker lock. A rejecting or throwing
                // scheduler leaves ownership with this thread, which completes the reserved pass
                // inline. RunOnce also protects against a scheduler that invokes and then throws.
                accepted = _tryScheduleCleanup(RunOnce);
            }
            catch
            {
                // The inline owner below preserves cache progress and keeps Add/Remove atomic from
                // the caller's perspective.
            }

            if (!accepted)
                RunOnce();
        }

        void Cleanup(DateTime now)
        {
            List<EvictedValue> evictedValues = null;
            var lockTaken = false;
            var followUpAt = default(DateTime);
            var scheduleFollowUp = false;
            try
            {
                Monitor.Enter(_lock, ref lockTaken);

                var itemsAboveCapacity = Statistics.Count - Statistics.Capacity;
                Bucket<TValue> bucket = _buckets[OldestBucketIndex];

                var expiration = now - _maxAge;
                var aged = now - _minAge;
                while (AreLowOnBuckets
                       || bucket.HasExpired(expiration)
                       || (itemsAboveCapacity > 0 && bucket.IsOldEnough(aged)))
                {
                    IBucketNode<TValue> node = bucket.Head;

                    bucket.Clear();

                    while (node != null)
                    {
                        IBucketNode<TValue> next = node.Pop();

                        if (node.IsValid && node.Bucket != null)
                        {
                            // if the node is in its original bucket, it's ripe for the pickin
                            if (node.Bucket == bucket)
                            {
                                if (node.TryEvict(out TValue value))
                                {
                                    --itemsAboveCapacity;
                                    Statistics.ValueRemoved();
                                    (evictedValues ??= []).Add(new EvictedValue(node, value));
                                }
                            }
                            else
                            {
                                // push it onto the bucket that now contains it
                                node.Bucket.Push(node);
                            }
                        }

                        node = next;
                    }

                    if (IsCurrentBucketOldest)
                        break;

                    bucket = _buckets[++OldestBucketIndex];
                }

                if (CanOpenBucketWithoutForcingEviction)
                    OpenBucket(++CurrentBucketIndex, now);
            }
            finally
            {
                if (lockTaken)
                {
                    // Publish completion and reserve any requested follow-up atomically. Additions
                    // that arrived while this pass was queued must not lose their cleanup signal.
                    scheduleFollowUp = _cleanupRequested;
                    followUpAt = _cleanupRequestedAt;
                    _cleanupRequested = false;
                    _cleanupScheduled = scheduleFollowUp;
                    Monitor.Exit(_lock);
                }
                else
                    Volatile.Write(ref _cleanupScheduled, false);
            }

            // Index and user observers are external lock owners. Publish only after the tracker
            // state is committed and its lock is released, preserving a single lock order.
            if (evictedValues is not null)
            {
                foreach (EvictedValue evicted in evictedValues)
                    PublishEviction(evicted.Node, evicted.Value);
            }

            if (scheduleFollowUp)
                DispatchCleanup(() => Cleanup(followUpAt));
        }

        List<EvictedValue> ResetCache(DateTime now)
        {
            IReadOnlyList<(IBucketNode<TValue> Node, TValue Value)> resetValues = _buckets.Empty();
            var evictedValues = new List<EvictedValue>(resetValues.Count);
            foreach ((IBucketNode<TValue> node, TValue value) in resetValues)
                evictedValues.Add(new EvictedValue(node, value));

            _buckets = new BucketCollection<TValue>(this, _bucketCount);
            _cacheResetTime = now.Add(TimeSpan.FromMilliseconds(MaxAgeUpperLimit));

            OldestBucketIndex = 0;
            OpenBucket(0, now);

            Statistics.Reset();

            return evictedValues;
        }

        static bool TryScheduleOnThreadPool(Action cleanup)
        {
            try
            {
                _ = Task.Run(cleanup);
                return true;
            }
            catch
            {
                return false;
            }
        }

        void PublishEviction(IBucketNode<TValue> node, TValue value)
        {
            DetachUsageNotification(node, value);

            try
            {
                _observers.ValueRemoved(node, value);
            }
            catch
            {
                // Observer fan-out is best effort after all observers, including indices, have
                // received the event. Cleanup progress must not be abandoned by user callbacks.
            }

            _ = DisposeEvictedValue(value);
        }

        static void ScheduleResetRelease(IReadOnlyList<EvictedValue> evictedValues)
        {
            if (evictedValues.Count == 0)
                return;

            // Clear is explicitly non-blocking. One background batch avoids a task per cached
            // value while still isolating disposal failures per item.
            _ = Task.Run(async () =>
            {
                foreach (EvictedValue evicted in evictedValues)
                {
                    DetachUsageNotification(evicted.Node, evicted.Value);
                    await DisposeEvictedValue(evicted.Value).ConfigureAwait(false);
                }
            });
        }

        static async Task DisposeEvictedValue(TValue value)
        {
            try
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
            catch
            {
                //
            }
        }

        static void DetachUsageNotification(IBucketNode<TValue> node, TValue value)
        {
            if (node is not BucketNode<TValue> bucketNode)
                return;

            try
            {
                // Event accessors are user code. The atomic eviction is complete before this
                // callback and no tracker lock is held while it runs.
                bucketNode.DetachUsageNotification(value);
            }
            catch
            {
                // A hostile event accessor cannot roll back eviction or block index publication.
            }
        }

        readonly record struct EvictedValue(IBucketNode<TValue> Node, TValue Value);
    }
}
