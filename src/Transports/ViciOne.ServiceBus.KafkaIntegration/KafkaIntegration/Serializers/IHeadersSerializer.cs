// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration.Serializers
{
    using Confluent.Kafka;


    public interface IHeadersSerializer
    {
        Headers Serialize(SendContext context);
    }
}
