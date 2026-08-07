// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    using Transports;


    public interface IKafkaMessageReceiver :
        IAgent,
        DeliveryMetrics
    {
    }


    public interface IKafkaMessageConsumer<TKey, TValue> :
        IKafkaMessageReceiver
        where TValue : class
    {

    }
}
