// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration.Serializers
{
    using System.Net.Mime;
    using Confluent.Kafka;


    public interface IKafkaSerializerFactory
    {
        ContentType ContentType { get; }
        IDeserializer<T> GetDeserializer<T>();
        IAsyncSerializer<T> GetSerializer<T>();
    }
}
