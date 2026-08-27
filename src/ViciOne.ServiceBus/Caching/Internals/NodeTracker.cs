namespace ViciOne.ServiceBus.Caching.Internals
{
    using System;
    using System.Collections.Generic;
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

            _observers = new CacheValueObservable<TValue>();

            var maxAgeInMilliseconds = Math.Min(settings.MaxAge.TotalMilliseconds, MaxAgeUpperLimit);

            _minAge = settings.MinAge;
            _maxAge = TimeSpan.FromMilliseconds(maxAgeInMilliseconds);

            _validityCheckInterval = TimeSpan.FromMilliseconds(maxAgeInMilliseconds / settings.TimeSlots);
            _cacheResetTime = _nowProvider().Add(TimeSpan.FromMilliseconds(MaxAgeUpperLimit));

            _bucketSize = Math.Max(settings.Capacity / settings.BucketCount, 1);

            // enough buckets for all of the content within the time slices, plus a few spares
            _bucketCount = settings.TimeSlots * settings.BucketCount + 5;

            _buckets = new BucketCollection<TValue>(this, _bucketCount);

            Statistics = new CacheStatistics(settings.Capacity, _bucketCount, _bucketSize, _minAge, _maxAge, _validityCheckInterval);

            OpenBucket(0);
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
            lock (_lock)
            {
                _buckets.Empty();

                _buckets = new BucketCollection<TValue>(this, _bucketCount);

                _cacheResetTime = _nowProvider().Add(TimeSpan.FromMilliseconds(MaxAgeUpperLimit));

                OldestBucketIndex = 0;

                OpenBucket(0);

                Statistics.Reset();

                _observers.CacheCleared();
            }
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
            var node = new BucketNode<TValue>(value);
            Action cleanup;

            lock (_lock)
            {
                _currentBucket.Push(node);

                Statistics.ValueAdded();

                cleanup = CheckCacheStatus();
            }

            try
            {
                _observers.ValueAdded(node, value);
            }
            finally
            {
                // Once the reservation is published, observer failures must not strand it.
                DispatchCleanup(cleanup);
            }
        }

        async Task RemoveNode(IBucketNode<TValue> node)
        {
            try
            {
                if (!node.TryEvict(out TValue value))
                    return;

                Statistics.ValueRemoved();

                Action cleanup = CheckCacheStatus();

                try
                {
                    _observers.ValueRemoved(node, value);
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
            }
            catch
            {
                //
            }
        }

        void OpenBucket(int index)
        {
            var lockTaken = false;

            try
            {
                Monitor.Enter(_lock, ref lockTaken);

                var now = _nowProvider();

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

        Action CheckCacheStatus()
        {
            lock (_lock)
            {
                var now = _nowProvider();

                if (!IsCleanupRequired(now))
                    return null;

                if (_cleanupScheduled)
                {
                    _cleanupRequested = true;

                    // Preserve size and time segmentation while the reserved cleanup waits, but
                    // never wrap onto a bucket that the cleanup has not reclaimed yet.
                    if (!AreLowOnBuckets)
                        OpenBucket(++CurrentBucketIndex);

                    return null;
                }

                if (CurrentBucketIndex > 1000000000 || now >= _cacheResetTime)
                {
                    Clear();
                    return null;
                }
                else
                {
                    Volatile.Write(ref _cleanupScheduled, true);

                    return () => Cleanup(now);
                }
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
            var lockTaken = false;
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
                                --itemsAboveCapacity;

                                // so if we don't await this, can't be too bad can it?
                                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                                EvictNode(node);
                                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                            }
                            else
                            {
                                // push it onto the bucket that now contains it
                                node.Bucket.Push(node);
                            }
                        }

                        node = next;
                    }

                    bucket = _buckets[++OldestBucketIndex];

                    if (IsCurrentBucketOldest)
                        break;
                }

                OpenBucket(++CurrentBucketIndex);
            }
            finally
            {
                if (lockTaken)
                {
                    // Publish completion and reserve any requested follow-up atomically. Additions
                    // that arrived while this pass was queued must not lose their cleanup signal.
                    scheduleFollowUp = _cleanupRequested;
                    _cleanupRequested = false;
                    _cleanupScheduled = scheduleFollowUp;
                    Monitor.Exit(_lock);
                }
                else
                    Volatile.Write(ref _cleanupScheduled, false);
            }

            if (scheduleFollowUp)
                DispatchCleanup(() => Cleanup(_nowProvider()));
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

        async Task EvictNode(IBucketNode<TValue> node)
        {
            if (!node.TryEvict(out TValue value))
                return;

            Statistics.ValueRemoved();

            _observers.ValueRemoved(node, value);

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
    }
}
