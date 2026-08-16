namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Net.Mime;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework;
    using ViciOne.ServiceBus.Serialization;
    using TestFramework.Messages;


    [TestFixture]
    public class When_a_message_fails_to_deserialize_properly :
        InMemoryTestFixture
    {
        [Test]
        public void It_should_respond_with_a_serialization_fault()
        {
            Assert.That(async () => await _response, Throws.TypeOf<RequestFaultException>());
        }

        IRequestClient<PingMessage> _requestClient;
        #pragma warning disable NUnit1032
        Task<Response<PongMessage>> _response;
        #pragma warning restore NUnit1032

        [OneTimeSetUp]
        public void Setup()
        {
            _requestClient = CreateRequestClient<PingMessage>();

            _response = _requestClient.GetResponse<PongMessage>(new PingMessage());
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            Handler<PingMessage>(configurator, async context => throw new SerializationException("This is fine, forcing death"));
        }
    }


    /// <summary>
    /// A message whose body cannot be read by any deserializer of the endpoint must produce a receive fault that
    /// carries the identity of what failed, and its payload must not reach a regular consumer.
    ///
    /// The previous version only set the content type header of the send context and then awaited the fault without
    /// a single assertion. That could not work: setting the header alone leaves the body serialized by the endpoint
    /// serializer, and an unknown media type falls back to the default deserializer, so the envelope deserialized
    /// and the ping was consumed normally. The case therefore needed a debugger to appear to pass.
    ///
    /// Both halves are now stated explicitly. The content type is one that the endpoint provably does not register,
    /// and the body is written unchanged and is not an envelope, so the fallback deserializer fails as well.
    /// </summary>
    [TestFixture]
    public class When_a_message_has_an_unrecognized_body_format :
        InMemoryTestFixture
    {
        [Test]
        public async Task It_should_publish_a_fault()
        {
            var messageId = NewId.NextGuid();

            await InputQueueSendEndpoint.Send(new PingMessage(), context =>
            {
                context.MessageId = messageId;

                // Setting the serializer sets the content type of the message as well, so the header and the body
                // state the same unsupported format.
                context.Serializer = new CopyBodySerializer(UnsupportedContentType, new StringMessageBody(NotAnEnvelope));
            });

            ConsumeContext<ReceiveFault> faultContext = await _faulted;

            ReceiveFault fault = faultContext.Message;

            Assert.Multiple(() =>
            {
                Assert.That(fault.ContentType, Is.EqualTo(UnsupportedContentType.MediaType),
                    "The fault must name the content type that could not be read");
                Assert.That(fault.FaultedMessageId, Is.EqualTo(messageId),
                    "The fault must name the message that could not be read");
                Assert.That(fault.FaultId, Is.Not.EqualTo(Guid.Empty));
                Assert.That(fault.Host, Is.Not.Null);
                Assert.That(fault.Exceptions, Is.Not.Null.And.Length.EqualTo(1));
                Assert.That(fault.Exceptions[0].ExceptionType, Is.EqualTo(typeof(SerializationException).FullName),
                    "Reading the body must fail as a serialization error, not as something else");
                Assert.That(faultContext.SourceAddress, Is.EqualTo(InputQueueAddress),
                    "The fault must come from the endpoint that could not read the message");

                Assert.That(_pingHandled.IsCompleted, Is.False,
                    "The unreadable payload must not have been processed as a regular message");
            });
        }

        /// <summary>
        /// Registered by no serializer of a default in memory endpoint, which registers exactly one media type.
        /// </summary>
        static readonly ContentType UnsupportedContentType = new ContentType("application/vnd.vicione.servicebus+msgpack");

        /// <summary>
        /// Not an envelope, so the deserializer the endpoint falls back to cannot read it either.
        /// </summary>
        const string NotAnEnvelope = "<not-an-envelope/>";

        #pragma warning disable NUnit1032
        Task<ConsumeContext<ReceiveFault>> _faulted;
        Task<ConsumeContext<PingMessage>> _pingHandled;
        #pragma warning restore NUnit1032

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            // Kept, not discarded: it is the evidence that the payload was never processed as a regular message.
            _pingHandled = Handled<PingMessage>(configurator);

            _faulted = Handled<ReceiveFault>(configurator);
        }
    }
}
