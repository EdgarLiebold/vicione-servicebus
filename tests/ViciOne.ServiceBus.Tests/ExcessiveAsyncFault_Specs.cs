// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests
{
    namespace NoLog
    {
        using System.Collections.Concurrent;
        using System.Linq;
        using System.Threading;
        using System.Threading.Tasks;
        using NUnit.Framework;
        using TestFramework;
        using TestFramework.Messages;
        using ViciOne.ServiceBus.Internals;


        /// <summary>
        /// Every message of a fault storm produces exactly one fault that carries the exception of its consumer.
        ///
        /// The previous version sent a thousand messages into a consumer that slept a hundred milliseconds, then
        /// read the collected faults through a blocking list whose deadline came from the wall clock, so the case
        /// silently returned a short array when the machine was slow. It also asserted the exception type through a
        /// projection whose result was discarded, so a fault carrying the wrong exception passed. The storm is now
        /// bounded by a declared concurrency, the last expected fault is the barrier, and both the count and the
        /// content of every fault are asserted.
        /// </summary>
        [TestFixture]
        public class An_excessive_fault_storm :
            InMemoryTestFixture
        {
            [Test]
            public async Task Should_publish_one_fault_for_every_message_in_the_storm()
            {
                for (var index = 0; index < StormSize; index++)
                    await InputQueueSendEndpoint.Send(new PingMessage());

                await _allFaults.Task.OrCanceled(TestCancellationToken);

                Fault<PingMessage>[] faults = _faults.ToArray();

                Assert.That(faults, Has.Length.EqualTo(StormSize));

                Assert.That(faults, Has.All.Matches<Fault<PingMessage>>(x =>
                    x.Exceptions.Length == 1
                    && x.Exceptions[0].ExceptionType == TypeCache<IntentionalTestException>.ShortName));
            }

            /// <summary>
            /// Large enough that the endpoint has to run faults and messages at the same time, small enough to stay
            /// a stated quantity rather than a load test.
            /// </summary>
            const int StormSize = 500;

            /// <summary>
            /// Declared, so how many deliveries overlap does not depend on the number of processors of the machine.
            /// </summary>
            const int ConcurrentDeliveries = 32;

            readonly TaskCompletionSource<bool> _allFaults;
            readonly ConcurrentBag<Fault<PingMessage>> _faults;
            int _faultCount;

            public An_excessive_fault_storm()
            {
                _faults = new ConcurrentBag<Fault<PingMessage>>();
                _allFaults = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
            {
                configurator.PrefetchCount = ConcurrentDeliveries;
                configurator.ConcurrentMessageLimit = ConcurrentDeliveries;

                configurator.Handler<Fault<PingMessage>>(context =>
                {
                    _faults.Add(context.Message);

                    if (Interlocked.Increment(ref _faultCount) == StormSize)
                        _allFaults.TrySetResult(true);

                    return Task.CompletedTask;
                });

                configurator.Consumer<MessageConsumer>();
            }


            public class MessageConsumer :
                IConsumer<PingMessage>
            {
                public Task Consume(ConsumeContext<PingMessage> context)
                {
                    throw new IntentionalTestException("Time for crunchin'");
                }
            }
        }
    }
}
