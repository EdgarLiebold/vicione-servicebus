// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Tests
{
    using System;
    using System.Threading.Tasks;
    using Consumers;
    using Contracts;
    using Futures;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Services;


    public class FryFuture_Specs :
        FutureTestFixture
    {
        public FryFuture_Specs(IFutureTestFixtureConfigurator testFixtureConfigurator)
            : base(testFixtureConfigurator)
        {
        }

        [Test]
        public async Task Should_complete()
        {
            var orderId = NewId.NextGuid();
            var orderLineId = NewId.NextGuid();

            var startedAt = DateTime.UtcNow;

            var scope = Provider.CreateScope();

            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<OrderFry>>();

            Response<FryCompleted> response = await client.GetResponse<FryCompleted>(new
            {
                OrderId = orderId,
                OrderLineId = orderLineId,
                Size = Size.Medium
            });

            Assert.Multiple(() =>
            {
                Assert.That(response.Message.OrderId, Is.EqualTo(orderId));
                Assert.That(response.Message.OrderLineId, Is.EqualTo(orderLineId));
                Assert.That(response.Message.Size, Is.EqualTo(Size.Medium));
                Assert.That(response.Message.Created, Is.GreaterThanOrEqualTo(startedAt));
                Assert.That(response.Message.Completed, Is.GreaterThanOrEqualTo(response.Message.Created));
            });
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<IFryer, Fryer>();
        }

        protected override void ConfigureViciOneServiceBus(IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<CookFryConsumer, CookFryConsumerDefinition>();
            configurator.AddFuture<FryFuture>();
        }
    }
}
