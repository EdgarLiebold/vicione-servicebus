namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System.Threading.Tasks;
    using Apache.NMS;
    using NUnit.Framework;


    [TestFixture(ActiveMqHostAddress.ActiveMqScheme)]
    [TestFixture(ActiveMqHostAddress.AmqpScheme)]
    public class InvalidMessage_Specs :
        ActiveMqTestFixture
    {
        public InvalidMessage_Specs(string protocol)
            : base(protocol)
        {
        }

        [Test]
        public async Task Should_fault()
        {
            Task<ConsumeContext<ReceiveFault>> receiveFault = await ConnectPublishHandler<ReceiveFault>();

            await ProduceInvalidMessage();

            await receiveFault;
        }

        protected override void ConfigureActiveMqReceiveEndpoint(IActiveMqReceiveEndpointConfigurator configurator)
        {
            Handled<SubmitOrder>(configurator);
        }

        async Task ProduceInvalidMessage()
        {
            // ActiveMqTransportOptions carries the library defaults, which point at localhost:61616
            // with a well known account. This spec produces a raw message straight onto the broker,
            // so it has to address the fixture the runner started, not whatever holds the default port.
            var brokerAddress = $"activemq:tcp://{RunScopedBroker.Host}:{RunScopedBroker.OpenWirePort}";

            var factory = new NMSConnectionFactory(brokerAddress);

            var connection = factory.ConnectionFactory.CreateConnection(RunScopedBroker.User, RunScopedBroker.Pass);
            try
            {
                var session = connection.CreateSession(AcknowledgementMode.ClientAcknowledge);

                var producer = session.CreateProducer(session.GetQueue(ActiveMqTestHarness.InputQueueName));

                var message = session.CreateMessage();

                message.NMSCorrelationID = "AB76E632-8550-49B9-A119-BBEB84D53355";

                await producer.SendAsync(message);

                await producer.CloseAsync();
                session.Close();
            }
            finally
            {
                connection.Close();
            }
        }


        class SubmitOrder
        {
            public string OrderId { get; set; }
        }
    }
}
