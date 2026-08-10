// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Testing;
    using NUnit.Framework;


    [TestFixture]
    public class Failure_Specs
    {
        /// <summary>
        /// A second bus that claims a bus endpoint queue another connection already holds exclusively
        /// must fail, and say so.
        /// <para>
        /// The spec used to assert that harness2.Start() throws. It cannot: the bus endpoint is
        /// materialised on demand, so Start() never declares the exclusive queue and never meets the
        /// conflict. Measured, the queue does not exist in the virtual host after either start, and the
        /// second start returns normally. Upstream MassTransit carries this spec as [Explicit], so the
        /// expectation never ran against a broker there either.
        /// </para>
        /// <para>
        /// Authorised by the product owner on 2026-08-10, to be reported to the lead architect: the
        /// assertion moves to the point where the conflict can arise, which is where a consumer is
        /// connected and the endpoint really comes up. The on-demand behaviour is left alone, because it
        /// is deliberate. The type asserted is exact, not relaxed to a base type.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_properly_fail_on_exclusive_launch()
        {
            var harness1 = new RabbitMqTestHarness();
            harness1.OnConfigureRabbitMqBus += configurator =>
            {
                configurator.OverrideDefaultBusEndpointQueueName("exclusively-yours");
                configurator.Exclusive = true;
            };

            var harness2 = new RabbitMqTestHarness();
            harness2.OnConfigureRabbitMqBus += configurator =>
            {
                configurator.OverrideDefaultBusEndpointQueueName("exclusively-yours");
                configurator.Exclusive = true;
            };

            // A harness left over from an earlier run would still hold the exclusivity and make this
            // pass for the wrong reason.
            await harness1.RecreateVirtualHost();
            await harness1.Start();

            // Materialises the bus endpoint, so its exclusive queue is really held by this connection.
            harness1.SubscribeHandler<TestFramework.Messages.PingMessage>();
            await Task.Delay(2000);

            try
            {
                await harness2.Start();

                Assert.That(() => harness2.SubscribeHandler<TestFramework.Messages.PingMessage>(),
                    Throws.TypeOf<ConnectionException>(),
                    "the second bus reported no conflict for a bus endpoint queue it cannot have");

                await harness2.Stop();
            }
            finally
            {
                await Task.Delay(1000);

                await harness1.Stop();
            }
        }
    }
}
