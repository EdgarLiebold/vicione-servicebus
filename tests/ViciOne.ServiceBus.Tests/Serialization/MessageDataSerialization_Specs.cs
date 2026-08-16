namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.MessageData;
    using ViciOne.ServiceBus.Serialization;
    using NUnit.Framework;


    [TestFixture(typeof(SystemTextJsonMessageSerializer))]
    [TestFixture(typeof(MessagePackMessageSerializer))]
    public class Serialization_a_message_data_property :
        SerializationTest
    {
        [Test]
        public async Task Should_handle_a_string_null()
        {
            var repository = new InMemoryMessageDataRepository();

            var dataId = new InMemoryMessageDataId().Uri;

            var obj = new SampleMessage {Value = await repository.PutString(new string('*', MessageDataDefaults.Threshold + 100))};

            Serialize(obj);
        }


        public class SampleMessage
        {
            public MessageData<string> Value { get; set; }
        }


        public Serialization_a_message_data_property(Type serializerType)
            : base(serializerType)
        {
        }
    }
}
