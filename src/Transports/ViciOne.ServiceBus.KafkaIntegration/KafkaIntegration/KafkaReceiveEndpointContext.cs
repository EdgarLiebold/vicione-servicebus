// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using Confluent.Kafka;
    using Serializers;
    using Transports;


    public interface KafkaReceiveEndpointContext<TKey, TValue> :
        ReceiveEndpointContext
        where TValue : class
    {
        string GroupId { get; }

        IHeadersDeserializer HeadersDeserializer { get; }
        IDeserializer<TKey> KeyDeserializer { get; }
        IDeserializer<TValue> ValueDeserializer { get; }
        IConsumerContextSupervisor ConsumerContextSupervisor { get; }

        KafkaTopicAddress GetInputAddress(string topic);
    }
}
