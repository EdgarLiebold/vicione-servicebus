// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework;


    [TestFixture]
    public class PublishStop_Specs
    {
        [Test]
        public async Task Should_start_and_stop_async()
        {
            // The host settings are derived from this URI, so it carries the run-scoped endpoint and
            // account. A bare "rabbitmq://localhost/..." would resolve to 5672 and guest.
            var queueUri = RunScopedBroker.QueueAddressWithCredentials("input_queue2");

            var rabbitMqHostSettings = queueUri.GetHostSettings();
            var receiveSettings = queueUri.GetReceiveSettings();

            var bus = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                sbc.Host(rabbitMqHostSettings);
                sbc.ReceiveEndpoint(receiveSettings.QueueName, ep =>
                {
                });
            });

            await bus.StartAsync();
            await bus.Publish(new DummyMessage { ID = 1 }).ConfigureAwait(false);
            await bus.StopAsync();
        }

        [Test]
        public async Task Should_start_and_stop_sync()
        {
            // The host settings are derived from this URI, so it carries the run-scoped endpoint and
            // account. A bare "rabbitmq://localhost/..." would resolve to 5672 and guest.
            var queueUri = RunScopedBroker.QueueAddressWithCredentials("input_queue2");

            var rabbitMqHostSettings = queueUri.GetHostSettings();
            var receiveSettings = queueUri.GetReceiveSettings();

            var bus = Bus.Factory.CreateUsingRabbitMq(sbc =>
            {
                BusTestFixture.ConfigureBusDiagnostics(sbc);

                sbc.Host(rabbitMqHostSettings);
                sbc.ReceiveEndpoint(receiveSettings.QueueName, ep =>
                {
                });
            });

            bus.Start();
            await bus.Publish(new DummyMessage { ID = 1 }).ConfigureAwait(false);
            bus.Stop();
        }


        class DummyMessage
        {
            public int ID { get; set; }
        }
    }
}
