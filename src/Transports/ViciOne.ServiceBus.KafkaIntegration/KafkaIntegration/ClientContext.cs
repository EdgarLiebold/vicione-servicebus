// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using Confluent.Kafka;


    public interface ClientContext :
        PipeContext
    {
        ClientConfig Config { get; }
    }
}
