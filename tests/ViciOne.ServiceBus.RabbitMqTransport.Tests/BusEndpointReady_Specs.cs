// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-10.
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
        static RabbitMqTestHarness ClaimingTheSameExclusiveBusEndpoint()
        {
            var harness = new RabbitMqTestHarness();
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
            await Task.Delay(2000);

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
                await Task.Delay(500);
                await first.Stop();
            }
        }
    }
}
