// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using KafkaIntegration;
    using Transports;


    public interface IKafkaRider :
        IRiderControl,
        ITopicProducerProvider,
        IKafkaTopicEndpointConnector
    {
    }
}
