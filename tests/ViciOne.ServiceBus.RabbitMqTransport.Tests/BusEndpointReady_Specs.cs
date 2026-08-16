namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework.Messages;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// Connecting a consumer to a bus whose endpoint can never become ready must not block forever.
    /// <para>
    /// The bus endpoint is materialised on demand: it declares its queue when something first consumes
    /// on the bus, not when the bus starts. Connecting a consumer is therefore the moment the endpoint
    /// has to come up, and the bus waits for it before handing the caller a subscription — which is
    /// right, because a subscription that is not live yet would silently drop messages.
    /// </para>
    /// <para>
    /// That wait was unbounded and could not be cancelled. If the endpoint can never start — here
    /// because a second connection already holds its exclusive queue — the caller blocked on its thread
    /// forever, with no exception, no timeout and no log line naming a cause. Measured against the
    /// pinned fixture: Start returned normally and the first ConnectHandler never came back, in two
    /// independent runs, until the run itself was killed.
    /// </para>
    /// <para>
    /// The assertion is deliberately weak about the outcome and strict about termination. Whether the
    /// broker conflict surfaces as one exception type or another is a separate question; that it
    /// surfaces at all rather than hanging is this fixture's subject.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Connecting_a_consumer_to_an_unavailable_bus_endpoint
    {
        /// <summary>
        /// A virtual host of this fixture's own.
        /// <para>
        /// An exclusively held queue is the one leftover the shared virtual host cannot clean up: the
        /// harness tears a run down by deleting every queue, and a queue another connection still holds
        /// refuses to be deleted. While the conflict took a full sixty seconds to surface this fixture
        /// ran long enough for the broker to have released it by the time the next one started. Once the
        /// conflict is reported in seconds, that accidental grace disappears — measured, the very next
        /// fixture then failed cleaning the shared virtual host. The name below removes the coupling
        /// instead of restoring the delay.
        /// </para>
        /// </summary>
        const string VirtualHost = "test-unavailable-bus-endpoint";

        static RabbitMqTestHarness ClaimingTheSameExclusiveBusEndpoint()
        {
            var harness = new RabbitMqTestHarness();

            harness.HostAddress = new UriBuilder(harness.HostAddress) { Path = $"/{VirtualHost}/" }.Uri;

            harness.OnConfigureRabbitMqBus += configurator =>
            {
                configurator.OverrideDefaultBusEndpointQueueName("exclusively-yours");
                configurator.Exclusive = true;
            };
            return harness;
        }

        [Test]
        public async Task Should_not_block_forever()
        {
            var first = ClaimingTheSameExclusiveBusEndpoint();

            // A harness left over from an earlier run could still hold the exclusivity, which would
            // make this pass for the wrong reason. The virtual host is recreated for this run.
            await first.RecreateVirtualHost();
            await first.Start();

            // Materialises the bus endpoint, so its exclusive queue is really held by this connection.
            first.SubscribeHandler<PingMessage>();

            // The broker is asked, not the clock. A sleep here claimed the queue was held and would
            // have gone on claiming it while the declare was still running, turning a broken
            // precondition into an unexplained timeout much later.
            await ExclusiveQueueProbe.WaitUntilHeld(first, "exclusively-yours");

            var second = ClaimingTheSameExclusiveBusEndpoint();
            try
            {
                await second.Start();

                // ConnectHandler blocks by design, so it is given its own thread and a bound. Without
                // the bound this test would hang instead of failing, which is exactly the defect.
                var connect = Task.Run(() => second.SubscribeHandler<PingMessage>());

                var completed = await Task.WhenAny(connect, Task.Delay(TimeSpan.FromSeconds(90)));

                Assert.That(completed, Is.SameAs(connect),
                    "connecting a consumer never returned, so the caller is blocked on a bus endpoint that cannot start");
            }
            finally
            {
                await second.Stop();
                await first.Stop();
            }
        }
    }
}
