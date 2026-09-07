using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Resolves Event Hubs producers by entity name.</summary>
public static class EventHubProducerExtensions
{
    /// <summary>Gets a producer for the named Event Hub.</summary>
    /// <param name="producerProvider">The provider from which to resolve the producer.</param>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="cancellationToken">The token that cancels producer resolution.</param>
    /// <returns>A task whose result is the producer for <paramref name="eventHubName" />.</returns>
    public static Task<IEventHubProducer> GetProducerAsync(this IEventHubProducerProvider producerProvider, string eventHubName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(producerProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventHubName);

        return producerProvider.GetProducerAsync(new Uri($"topic:{eventHubName}"), cancellationToken: cancellationToken);
    }
}
