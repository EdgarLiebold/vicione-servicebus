// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.KafkaIntegration.Caching
{
    using System;
    using ViciOne.ServiceBus.Caching;


    public interface ICachedTopicProducer<out T> :
        INotifyValueUsed,
        IAsyncDisposable
    {
        T Key { get; }
    }
}
