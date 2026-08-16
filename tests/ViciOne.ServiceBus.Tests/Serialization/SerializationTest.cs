namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using Context;
    using InMemoryTransport;
    using Internals;
    using ViciOne.ServiceBus.Serialization;
    using NUnit.Framework;
    using TestFramework;


    public abstract class SerializationTest :
        InMemoryTestFixture
    {
        readonly Uri _destinationAddress = new Uri("loopback://localhost/destination");
        readonly Uri _faultAddress = new Uri("loopback://localhost/fault");
        protected readonly Guid _requestId = Guid.NewGuid();
        readonly Uri _responseAddress = new Uri("loopback://localhost/response");
        readonly Type _serializerType;
        readonly Uri _sourceAddress = new Uri("loopback://localhost/source");
        protected IMessageDeserializer Deserializer;
        protected IMessageSerializer Serializer;

        public SerializationTest(Type serializerType)
        {
            _serializerType = serializerType;
        }

        [OneTimeSetUp]
        public void SetupSerializationTest()
        {
            if (_serializerType == typeof(SystemTextJsonMessageSerializer))
            {
                var serializer = new SystemTextJsonMessageSerializer();
                Serializer = serializer;
                Deserializer = serializer;
            }
            else if (_serializerType == typeof(SystemTextJsonRawMessageSerializer))
            {
                var serializer = new SystemTextJsonRawMessageSerializer();
                Serializer = serializer;
                Deserializer = serializer;
            }
            else if (_serializerType == typeof(MessagePackMessageSerializer))
            {
                var serializer = new MessagePackMessageSerializer();
                Serializer = serializer;
                Deserializer = serializer;
            }
            else
            {
                // A fixture that names a serializer this base does not build would otherwise run with a null
                // serializer and fail somewhere far away from the cause.
                throw new ArgumentException($"The serialization test does not build {_serializerType.Name}", nameof(_serializerType));
            }
        }

        protected T SerializeAndReturn<T>(T obj)
            where T : class
        {
            var serializedMessageData = Serialize(obj);

            return Return<T>(serializedMessageData);
        }

        protected byte[] Serialize<T>(T obj)
            where T : class
        {
            var sendContext = new MessageSendContext<T>(obj)
            {
                SourceAddress = _sourceAddress,
                DestinationAddress = _destinationAddress,
                FaultAddress = _faultAddress,
                ResponseAddress = _responseAddress,
                RequestId = _requestId
            };

            var serializedMessageData = Serializer.GetMessageBody(sendContext).GetBytes();

            return serializedMessageData;
        }

        protected T Return<T>(byte[] serializedMessageData)
            where T : class
        {
            var message = new InMemoryTransportMessage(Guid.NewGuid(), serializedMessageData, Serializer.ContentType.MediaType);
            var receiveContext = new InMemoryReceiveContext(message, TestConsumeContext.GetContext());

            var consumeContext = Deserializer.Deserialize(receiveContext);

            consumeContext.TryGetMessage(out ConsumeContext<T> messageContext);

            Assert.That(messageContext, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(messageContext.SourceAddress, Is.EqualTo(_sourceAddress));
                Assert.That(messageContext.DestinationAddress, Is.EqualTo(_destinationAddress));
                Assert.That(messageContext.FaultAddress, Is.EqualTo(_faultAddress));
                Assert.That(messageContext.ResponseAddress, Is.EqualTo(_responseAddress));
                Assert.That(messageContext.RequestId.HasValue, Is.EqualTo(true));
                Assert.That(messageContext.RequestId.Value, Is.EqualTo(_requestId));
            });

            return messageContext.Message;
        }

        protected virtual void TestSerialization<T>(T message)
            where T : class
        {
            var result = SerializeAndReturn(message);

            Assert.That(message, Is.EqualTo(result));
        }
    }
}
