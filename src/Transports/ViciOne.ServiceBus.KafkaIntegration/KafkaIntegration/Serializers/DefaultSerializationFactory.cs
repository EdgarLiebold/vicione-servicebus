// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration.Serializers
{
    using System.Net.Mime;
    using Confluent.Kafka;
    using Serialization;


    public class DefaultKafkaSerializerFactory :
        IKafkaSerializerFactory
    {
        public ContentType ContentType => SystemTextJsonMessageSerializer.JsonContentType;

        public IDeserializer<T> GetDeserializer<T>()
        {
            return DeserializerTypes.TryGet<T>() ?? new ViciOneServiceBusJsonDeserializer<T>();
        }

        public IAsyncSerializer<T> GetSerializer<T>()
        {
            return SerializerTypes.TryGet<T>() ?? new ViciOneServiceBusAsyncJsonSerializer<T>();
        }
    }
}
