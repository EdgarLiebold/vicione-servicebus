namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Serialization;
    using NUnit.Framework;
    using TestFramework;
    using TestFramework.Messages;


    [TestFixture]
    /// <summary>
    /// One bus, two serializers: the endpoint answers with a different one than the bus publishes
    /// with. The pair used to be the envelope and BSON; BSON went with the removed library, so the
    /// second serializer is now raw JSON. What is asserted is unchanged — the two content types have
    /// to differ and each has to be the one its side configured.
    /// </summary>
    public class SeparateSerializer_Specs :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_handle_both_serializers()
        {
            Task<ConsumeContext<PongMessage>> ponged = await ConnectPublishHandler<PongMessage>();

            await Bus.Publish(new PingMessage());

            ConsumeContext<PingMessage> pingContext = await _handled;

            Assert.That(pingContext.ReceiveContext.ContentType, Is.EqualTo(SystemTextJsonMessageSerializer.JsonContentType),
                $"actual ping type is {pingContext.ReceiveContext.ContentType}");

            ConsumeContext<PongMessage> pongContext = await ponged;

            Assert.That(pongContext.ReceiveContext.ContentType, Is.EqualTo(SystemTextJsonRawMessageSerializer.JsonContentType),
                $"actual type is {pongContext.ReceiveContext.ContentType}");
        }

        #pragma warning disable NUnit1032
        Task<ConsumeContext<PingMessage>> _handled;
        #pragma warning restore NUnit1032

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.UseRawJsonDeserializer();
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            base.ConfigureInMemoryReceiveEndpoint(configurator);

            configurator.UseRawJsonSerializer();

            _handled = Handler<PingMessage>(configurator, async context =>
            {
                await context.RespondAsync(new PongMessage(context.Message.CorrelationId));
            });
        }
    }


    [TestFixture]
    public class Sending_and_consuming_raw_json :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_handle_any_requested_message_type()
        {
            var message = new BagOfCrap
            {
                CommandId = NewId.NextGuid(),
                ItemNumber = "27"
            };

            await InputQueueSendEndpoint.Send(message);

            ConsumeContext<Command> context = await _handled;

            Assert.Multiple(() =>
            {
                Assert.That(context.ReceiveContext.ContentType, Is.EqualTo(SystemTextJsonRawMessageSerializer.JsonContentType),
                    $"unexpected content-type {context.ReceiveContext.ContentType}");

                Assert.That(context.Message.CommandId, Is.EqualTo(message.CommandId));
                Assert.That(context.Message.ItemNumber, Is.EqualTo(message.ItemNumber));
            });
        }

        #pragma warning disable NUnit1032
        Task<ConsumeContext<Command>> _handled;
        #pragma warning restore NUnit1032

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.UseRawJsonSerializer(RawSerializerOptions.All);
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            base.ConfigureInMemoryReceiveEndpoint(configurator);

            _handled = Handled<Command>(configurator);
        }


        public interface Command
        {
            Guid CommandId { get; }
            string ItemNumber { get; }
        }


        public class BagOfCrap
        {
            public Guid CommandId { get; set; }
            public string ItemNumber { get; set; }
        }
    }


}
