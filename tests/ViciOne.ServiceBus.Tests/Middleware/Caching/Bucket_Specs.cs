namespace ViciOne.ServiceBus.Tests.Middleware.Caching
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestValueObjects;
    using Util;
    using ViciOne.ServiceBus.Caching;
    using ViciOne.ServiceBus.Caching.Internals;


    /// <summary>
    /// The capacity of a green cache is a target, not a hard bound, and the tracker adds and sweeps on the
    /// thread pool. How many nodes a single sweep releases therefore depends on when its continuation runs,
    /// and no exact end count is promised. These fixtures wait for confirmed observer events and then assert
    /// the effect the cache does promise. CancelAfter bounds a stalled wait so that a missing event fails the
    /// test instead of stopping the run; it is not the synchronisation.
    /// </summary>
    [TestFixture]
    public class Adding_nodes_to_a_bucket
    {
        const int WaitMilliseconds = 30000;
        const int Capacity = 100;

        [Test]
        [CancelAfter(WaitMilliseconds)]
        public async Task Should_add_the_first_node(CancellationToken cancellationToken)
        {
            var settings = new CacheSettings(1000, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            var manager = new NodeTracker<SimpleValue>(settings);

            var bucket = new Bucket<SimpleValue>(manager);

            var valueNode = new BucketNode<SimpleValue>(await SimpleValueFactory.Healthy("Hello"));
            bucket.Push(valueNode);

            Assert.Multiple(() =>
            {
                Assert.That(bucket.Count, Is.EqualTo(1), "the pushed node did not reach the bucket");
                Assert.That(bucket.Head, Is.SameAs(valueNode), "the pushed node is not the head of the bucket");
                Assert.That(valueNode.Bucket, Is.SameAs(bucket), "the node does not know the bucket it was pushed into");
            });
        }

        [Test]
        [CancelAfter(WaitMilliseconds)]
        public async Task Should_fill_up_the_buckets(CancellationToken cancellationToken)
        {
            // As many values as the capacity allows and a clock that never moves: nothing is eligible for
            // removal, so this end count is promised rather than scheduled.
            var settings = new TestCacheSettings(Capacity, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));

            var cache = new GreenCache<SimpleValue>(settings);

            IIndex<string, SimpleValue> index = cache.AddIndex("id", x => x.Id);

            var observer = new NodeCountObserver<SimpleValue>(Capacity);
            cache.Connect(observer);

            for (var i = 0; i < Capacity; i++)
                await index.Get($"key{i}", SimpleValueFactory.Healthy);

            await observer.Added.WaitAsync(cancellationToken);

            Assert.Multiple(() =>
            {
                Assert.That(cache.Statistics.Count, Is.EqualTo(Capacity), "nothing was eligible for removal, so every added value must still be held");
                Assert.That(observer.RemovedCount, Is.Zero, "a value was removed although none had aged and the capacity was not exceeded");
            });
        }

        [Test]
        [CancelAfter(WaitMilliseconds)]
        public async Task Should_fill_up_the_buckets_over_time(CancellationToken cancellationToken)
        {
            // A hundred seconds of cache time pass, all of them well inside the three hundred second maximum
            // age, so again nothing may be removed.
            var settings = new TestCacheSettings(Capacity, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(300));

            var cache = new GreenCache<SimpleValue>(settings);

            IIndex<string, SimpleValue> index = cache.AddIndex("id", x => x.Id);

            var observer = new NodeCountObserver<SimpleValue>(Capacity);
            cache.Connect(observer);

            for (var i = 0; i < Capacity; i++)
            {
                await index.Get($"key{i}", SimpleValueFactory.Healthy);

                settings.CurrentTime += TimeSpan.FromSeconds(1);
            }

            await observer.Added.WaitAsync(cancellationToken);

            Assert.Multiple(() =>
            {
                Assert.That(cache.Statistics.Count, Is.EqualTo(Capacity), "nothing exceeded the maximum age, so every added value must still be held");
                Assert.That(observer.RemovedCount, Is.Zero, "a value was removed although none had aged out");
            });
        }

        [Test]
        [CancelAfter(WaitMilliseconds)]
        public async Task Should_fill_up_a_bunch_of_buckets(CancellationToken cancellationToken)
        {
            // Removal by age, not by capacity: a hundred values enter over a hundred seconds of cache time
            // and the maximum age is sixty, so the older part of them has to leave again.
            const int AgedOut = 35;

            var settings = new TestCacheSettings(Capacity, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));

            var cache = new GreenCache<SimpleValue>(settings);

            IIndex<string, SimpleValue> index = cache.AddIndex("id", x => x.Id);

            var observer = new NodeCountObserver<SimpleValue>(Capacity, AgedOut);
            cache.Connect(observer);

            for (var i = 0; i < Capacity; i++)
            {
                await index.Get($"key{i}", SimpleValueFactory.Healthy);

                settings.CurrentTime += TimeSpan.FromSeconds(1);
            }

            await observer.Added.WaitAsync(cancellationToken);
            await observer.Removed.WaitAsync(cancellationToken);

            AssertBoundedAndNotEmpty(cache, settings);

            Assert.That(cache.Statistics.Count, Is.LessThan(Capacity),
                "the cache still holds every value although values older than the maximum age were removed");
        }

        [Test]
        [CancelAfter(WaitMilliseconds)]
        public async Task Should_fill_them_even_fuller(CancellationToken cancellationToken)
        {
            var settings = new TestCacheSettings(Capacity, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));

            var cache = new GreenCache<SimpleValue>(settings);

            IIndex<string, SimpleValue> index = cache.AddIndex("id", x => x.Id);

            var observer = new NodeCountObserver<SimpleValue>(200);
            cache.Connect(observer);

            var added = await FillUntilBelowCapacity(index, SimpleValueFactory.Healthy, observer, settings, 200,
                i => i % 2 == 0, cancellationToken);

            await observer.Added.WaitAsync(cancellationToken);

            AssertShrunkToCapacity(cache, settings, observer, added);
        }

        [Test]
        [CancelAfter(WaitMilliseconds)]
        public async Task Should_fill_up_the_buckets_over_time_and_remove_old_entries(CancellationToken cancellationToken)
        {
            var settings = new TestCacheSettings(Capacity, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(300));

            var cache = new GreenCache<SimpleValue>(settings);

            IIndex<string, SimpleValue> index = cache.AddIndex("id", x => x.Id);

            var observer = new NodeCountObserver<SimpleValue>(200);
            cache.Connect(observer);

            var added = await FillUntilBelowCapacity(index, SimpleValueFactory.Healthy, observer, settings, 200,
                _ => true, cancellationToken);

            await observer.Added.WaitAsync(cancellationToken);

            AssertShrunkToCapacity(cache, settings, observer, added);
        }

        [Test]
        [CancelAfter(WaitMilliseconds)]
        public async Task Should_fill_up_the_buckets_with_smart_values_over_time_and_remove_old_entries(CancellationToken cancellationToken)
        {
            var settings = new TestCacheSettings(Capacity, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(300));

            var cache = new GreenCache<SmartValue>(settings);

            IIndex<string, SmartValue> index = cache.AddIndex("id", x => x.Id);

            var observer = new NodeCountObserver<SmartValue>(200);
            cache.Connect(observer);

            var added = await FillUntilBelowCapacity(index, SmartValueFactory.Healthy, observer, settings, 200,
                _ => true, cancellationToken);

            await observer.Added.WaitAsync(cancellationToken);

            AssertShrunkToCapacity(cache, settings, observer, added);
        }

        /// <summary>
        /// Offers values until the cache has confirmed enough removals to be at or below its capacity, and at
        /// least the requested minimum has been offered. The cache holds exactly what it took in minus what it
        /// gave up, so this condition is what makes the capacity bound hold instead of an assumption about
        /// when a sweep happens to run. Every step waits on real work; nothing sleeps and nothing reads a clock.
        /// </summary>
        static async Task<int> FillUntilBelowCapacity<T>(IIndex<string, T> index, MissingValueFactory<string, T> factory,
            NodeCountObserver<T> observer, TestCacheSettings settings, int minimumAdded, Func<int, bool> advanceTime,
            CancellationToken cancellationToken)
            where T : class
        {
            var added = 0;
            while (added < minimumAdded || observer.RemovedCount < added - settings.Capacity)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await index.Get($"key{added}", factory);

                if (advanceTime(added))
                    settings.CurrentTime += TimeSpan.FromSeconds(1);

                added++;
            }

            return added;
        }

        static void AssertShrunkToCapacity<T>(GreenCache<T> cache, CacheSettings settings, NodeCountObserver<T> observer, int added)
            where T : class
        {
            Assert.Multiple(() =>
            {
                Assert.That(observer.AddedCount, Is.GreaterThanOrEqualTo(200), "fewer than two hundred additions were confirmed");
                Assert.That(observer.RemovedCount, Is.GreaterThanOrEqualTo(added - settings.Capacity),
                    "the cache reported fewer removals than it needs to be at its capacity");
                Assert.That(observer.RemovedCount, Is.GreaterThanOrEqualTo(100), "fewer than a hundred removals were confirmed");
            });

            AssertBoundedAndNotEmpty(cache, settings);
        }

        static void AssertBoundedAndNotEmpty<T>(GreenCache<T> cache, CacheSettings settings)
            where T : class
        {
            var count = cache.Statistics.Count;
            Task<T>[] values = cache.GetAll().ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(count, Is.GreaterThan(0), "the cache dropped everything instead of shrinking to its capacity");
                Assert.That(count, Is.LessThanOrEqualTo(settings.Capacity), "the cache holds more than its capacity after the removals were confirmed");
                Assert.That(values, Is.Not.Empty, "GetAll returned nothing although the cache reports held values");
                Assert.That(values, Has.Length.LessThanOrEqualTo(settings.Capacity), "GetAll returned more values than the capacity allows");
            });
        }


        /// <summary>
        /// Completes once at least the expected number of events has been observed. An observer that completes
        /// on an exact count never completes when one event more arrives, which turns a scheduling difference
        /// into a test that hangs rather than one that fails.
        /// </summary>
        class NodeCountObserver<T> :
            ICacheValueObserver<T>
            where T : class
        {
            readonly TaskCompletionSource<bool> _added;
            readonly int _expectedAdded;
            readonly int _expectedRemoved;
            readonly TaskCompletionSource<bool> _removed;
            int _addedCount;
            int _removedCount;

            public NodeCountObserver(int expectedAdded, int expectedRemoved = 0)
            {
                _expectedAdded = expectedAdded;
                _expectedRemoved = expectedRemoved;

                _added = TaskUtil.GetTask();
                _removed = TaskUtil.GetTask();

                if (expectedRemoved == 0)
                    _removed.SetCompleted();
            }

            public Task<bool> Added => _added.Task;
            public Task<bool> Removed => _removed.Task;

            public int AddedCount => Volatile.Read(ref _addedCount);
            public int RemovedCount => Volatile.Read(ref _removedCount);

            public void ValueAdded(INode<T> node, T value)
            {
                if (Interlocked.Increment(ref _addedCount) >= _expectedAdded)
                    _added.SetCompleted();
            }

            public void ValueRemoved(INode<T> node, T value)
            {
                if (Interlocked.Increment(ref _removedCount) >= _expectedRemoved)
                    _removed.SetCompleted();
            }

            public void CacheCleared()
            {
            }
        }
    }
}
