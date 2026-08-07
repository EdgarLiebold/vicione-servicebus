// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface ITopicProducerProvider :
        ISendObserverConnector
    {
        ITopicProducer<TKey, TValue> GetProducer<TKey, TValue>(Uri address)
            where TValue : class;
    }
}
