// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using NUnit.Framework;
    using Testing;


    [TestFixture]
    [Explicit]
    public class ExclusiveConsumer_Specs :
        RabbitMqTestFixture
    {
        protected override void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
            configurator.ExclusiveConsumer = true;
        }

        [Test]
        public async Task Should_not_be_allowed_twice()
        {
            Assert.That(async () =>
            {
                using var token = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                using var secondHarness = new RabbitMqTestHarness { CleanVirtualHost = false };
                secondHarness.OnConfigureRabbitMqBus += configurator =>
                {
                    ConfigureBusDiagnostics(configurator);
                };

                await secondHarness.Start(token.Token).OrCanceled(TestCancellationToken);

                await secondHarness.Stop();
            }, Throws.TypeOf<RabbitMqConnectionException>());
        }
    }
}
