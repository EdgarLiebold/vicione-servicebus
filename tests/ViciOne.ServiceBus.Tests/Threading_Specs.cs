namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using NUnit.Framework;
    using TestFramework;


    /// <summary>
    /// A receive endpoint that is configured for a high number of concurrent consumers must actually deliver that
    /// many messages at the same time.
    ///
    /// The previous version of this case measured the average latency of one hundred publishes with a stopwatch and
    /// printed it, while its bus configuration was commented out, so no consumer existed and the case could never
    /// pass. The contract underneath it is kept and made observable: every delivery is held inside the handler until
    /// all of them have arrived, so the number of deliveries that are simultaneously in flight is counted instead of
    /// timed. If the endpoint served the messages one after another, the first delivery would never return and the
    /// barrier would never be reached.
    /// </summary>
    [TestFixture]
    public class When_configuring_the_thread_pool_for_a_high_number_of_consumers :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_scale_threads_to_meet_demand()
        {
            for (var i = 0; i < ConsumerCount; i++)
                await Bus.Publish(new A());

            // Completes only when all one hundred deliveries are inside the handler at the same time. An endpoint
            // that serves fewer at once never reaches the barrier, so the peak it did reach is reported instead of
            // a bare cancellation.
            try
            {
                await _allEntered.Task.OrCanceled(TestCancellationToken);
            }
            catch (OperationCanceledException)
            {
                Assert.Fail($"Only {Volatile.Read(ref _peak)} of {ConsumerCount} deliveries ran at the same time");
            }

            Assert.That(Volatile.Read(ref _peak), Is.EqualTo(ConsumerCount),
                "The endpoint must run as many deliveries at once as the configured concurrency demands");

            _release.TrySetResult(true);

            await _allCompleted.Task.OrCanceled(TestCancellationToken);

            Assert.That(Volatile.Read(ref _completed), Is.EqualTo(ConsumerCount));
        }

        const int ConsumerCount = 100;

        readonly TaskCompletionSource<bool> _allCompleted;
        readonly TaskCompletionSource<bool> _allEntered;
        readonly TaskCompletionSource<bool> _release;
        int _completed;
        int _current;
        int _peak;

        public When_configuring_the_thread_pool_for_a_high_number_of_consumers()
        {
            _release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _allEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _allCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            configurator.PrefetchCount = ConsumerCount;
            configurator.ConcurrentMessageLimit = ConsumerCount;

            configurator.Handler<A>(async context =>
            {
                var current = Interlocked.Increment(ref _current);

                int peak;
                while (current > (peak = Volatile.Read(ref _peak)))
                {
                    if (Interlocked.CompareExchange(ref _peak, current, peak) == peak)
                        break;
                }

                if (current == ConsumerCount)
                    _allEntered.TrySetResult(true);

                await _release.Task.OrCanceled(context.CancellationToken);

                Interlocked.Decrement(ref _current);

                if (Interlocked.Increment(ref _completed) == ConsumerCount)
                    _allCompleted.TrySetResult(true);
            });
        }


        public class A
        {
        }
    }
}
