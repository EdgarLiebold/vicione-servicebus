// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NsbContracts;
    using NUnit.Framework;


    [TestFixture]
    public class Serializing_a_message_using_the_nsb_serializer
    {
        [Test]
        public async Task Should_only_trigger_one_consumer()
        {
            await using var provider = new ServiceCollection()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddConsumer<PlaceOrderConsumer>()
                        .Endpoint(e => e.Name = "single-queue");

                    x.AddConsumer<TerminateProcessConsumer>()
                        .Endpoint(e => e.Name = "single-queue");

                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.UseNServiceBusXmlSerializer();

                        cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            await harness.Bus.Publish(new PlaceOrder { OrderId = NewId.NextGuid() });

            await Assert.MultipleAsync(async () =>
            {
                Assert.That(await harness.GetConsumerHarness<PlaceOrderConsumer>().Consumed.Any<PlaceOrder>(), Is.True);
                Assert.That(await harness.GetConsumerHarness<TerminateProcessConsumer>().Consumed.Any<TerminateProcess>(), Is.False);
            });
        }


        class PlaceOrderConsumer :
            IConsumer<PlaceOrder>
        {
            public Task Consume(ConsumeContext<PlaceOrder> context)
            {
                return Task.CompletedTask;
            }
        }


        class TerminateProcessConsumer :
            IConsumer<TerminateProcess>
        {
            public Task Consume(ConsumeContext<TerminateProcess> context)
            {
                return Task.CompletedTask;
            }
        }
    }


    namespace NsbContracts
    {
        using System;


        public interface IMessage
        {
        }


        public interface ICommand :
            IMessage
        {
        }


        public class PlaceOrder :
            ICommand
        {
            public Guid OrderId { get; set; }
        }


        public class TerminateProcess :
            ICommand
        {
            string Reason { get; set; }
        }
    }
}
