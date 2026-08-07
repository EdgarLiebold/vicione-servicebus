// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;


    [TestFixture]
    public class PublisherConfirm_Specs :
        RabbitMqTestFixture
    {
        [Test]
        public async Task Should_call_the_ack_method_upon_delivery()
        {
            await InputQueueSendEndpoint.Send(new A { StringA = "ValueA" });

            await _received;
        }

        Task<ConsumeContext<A>> _received;

        protected override void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
            _received = Handled<A>(configurator);
        }


        class A
        {
            public string StringA { get; set; }
        }
    }
}
