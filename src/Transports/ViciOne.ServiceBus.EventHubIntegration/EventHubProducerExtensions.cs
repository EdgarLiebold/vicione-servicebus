using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public static class EventHubProducerExtensions
{
    public static Task<IEventHubProducer> GetProducerAsync(this IEventHubProducerProvider producerProvider, string eventHubName, CancellationToken cancellationToken = default)
    {
        if (producerProvider == null)
            throw new ArgumentNullException(nameof(producerProvider));
        if (string.IsNullOrWhiteSpace(eventHubName))
            throw new ArgumentNullException(nameof(eventHubName));

        return producerProvider.GetProducerAsync(new Uri($"topic:{eventHubName}"), cancellationToken: cancellationToken);
    }
}
