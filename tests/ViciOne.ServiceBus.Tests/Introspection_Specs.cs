// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Testing;
    using NUnit.Framework;
    using TestFramework;
    using TestFramework.Messages;


    [TestFixture]
    public class Probing_the_bus :
        InMemoryTestFixture
    {
        [Test]
        public void Should_extract_receive_endpoint_addresses()
        {
            List<Uri> receiveAddresses = Bus.GetReceiveEndpointAddresses().ToList();
            Assert.That(receiveAddresses, Does.Contain(InputQueueAddress));
        }

        #pragma warning disable NUnit1032
        Task<ConsumeContext<PingMessage>> _handled;
        #pragma warning restore NUnit1032

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            configurator.Handler<PingMessage>(async context =>
            {
            }, x =>
            {
                x.UseRateLimit(100, TimeSpan.FromSeconds(1));
                x.UseConcurrencyLimit(32);
            });

            _handled = Handled<PingMessage>(configurator);

            var consumer = new MultiTestConsumer(TestTimeout);

            consumer.Consume<PingMessage>();
            consumer.Consume<PongMessage>();

            consumer.Configure(configurator);
        }
    }
}
