namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework.Messages;


    [TestFixture]
    public class Publishing_a_message_in_a_consumer_with_baggage :
        RabbitMqTestFixture
    {
        ActivityListener _listener;

        /// <summary>
        /// Gives the spec the diagnostic context it is written around.
        /// <para>
        /// The subject here is baggage: the consumer adds it to the ambient activity and the published
        /// response is expected to carry it. In .NET an ActivitySource creates an activity only while
        /// something listens, so with no listener <c>Activity.Current</c> is null on both sides — the
        /// consumer's AddBaggage does nothing and the publish handler's filter can never match. The
        /// round trip then completes correctly and the test still waits out its full budget.
        /// </para>
        /// <para>
        /// Measured before this hook existed: the ping was consumed after 24 ms, the pong was published
        /// and received by the publish handler's endpoint after a further 5 ms, and the test failed 30 s
        /// later on the response it had already been handed. The message flow was never the problem.
        /// </para>
        /// <para>
        /// The suite offers nothing that would do this. BusTestFixture.ConfigureBusDiagnostics subscribes
        /// a DiagnosticListener observer behind the DIAG variable, which is a different mechanism and
        /// does not make an ActivitySource produce activities.
        /// </para>
        /// </summary>
        [SetUp]
        public void ListenForActivities()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };

            ActivitySource.AddActivityListener(_listener);
        }

        [TearDown]
        public void StopListeningForActivities()
        {
            _listener?.Dispose();
        }

        [Test]
        public async Task Should_source_address_from_the_endpoint()
        {
            Task<ConsumeContext<PongMessage>> responseHandled = await ConnectPublishHandler<PongMessage>(pongContext =>
            {
                return Activity.Current?.Baggage.Any(x => x.Key.Equals("Suitcase")) ?? false;
            });

            await InputQueueSendEndpoint.Send(new PingMessage());

            ConsumeContext<PingMessage> context = await _handled;

            ConsumeContext<PongMessage> responseContext = await responseHandled;
            Assert.That(responseContext.SourceAddress, Is.EqualTo(InputQueueAddress));
        }

        Task<ConsumeContext<PingMessage>> _handled;

        protected override void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
            _handled = Handler<PingMessage>(configurator, context =>
            {
                Activity.Current?.AddBaggage("Suitcase", "Full of cash");

                return context.Publish(new PongMessage(context.Message.CorrelationId));
            });
        }
    }
}
