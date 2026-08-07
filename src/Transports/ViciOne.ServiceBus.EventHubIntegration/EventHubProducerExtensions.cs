// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Threading.Tasks;


    public static class EventHubProducerExtensions
    {
        public static Task<IEventHubProducer> GetProducer(this IEventHubProducerProvider producerProvider, string eventHubName)
        {
            if (producerProvider == null)
                throw new ArgumentNullException(nameof(producerProvider));
            if (string.IsNullOrWhiteSpace(eventHubName))
                throw new ArgumentNullException(nameof(eventHubName));

            return producerProvider.GetProducer(new Uri($"topic:{eventHubName}"));
        }
    }
}
