using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides extension methods for event hub producer.
/// </summary>
public static class EventHubProducerExtensions
{
    /// <summary>
    /// Gets producer.
    /// </summary>
    /// <param name="producerProvider">The producer provider value.</param>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<IEventHubProducer> GetProducerAsync(this IEventHubProducerProvider producerProvider, string eventHubName, CancellationToken cancellationToken = default)
    {
        if (producerProvider == null)
            throw new ArgumentNullException(nameof(producerProvider));
        if (string.IsNullOrWhiteSpace(eventHubName))
            throw new ArgumentNullException(nameof(eventHubName));

        return producerProvider.GetProducerAsync(new Uri($"topic:{eventHubName}"), cancellationToken: cancellationToken);
    }
}
