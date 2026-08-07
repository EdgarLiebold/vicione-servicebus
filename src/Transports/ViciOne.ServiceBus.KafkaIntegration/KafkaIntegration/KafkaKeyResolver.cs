// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration
{
    public delegate TKey KafkaKeyResolver<out TKey, TValue>(KafkaSendContext<TValue> context)
        where TValue : class;
}
