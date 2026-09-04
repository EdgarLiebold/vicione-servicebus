using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for event hub producer provider.
/// </summary>
public interface IEventHubProducerProvider :
    ISendObserverConnector
{
    /// <summary>
    /// Gets producer.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<IEventHubProducer> GetProducerAsync(Uri address, CancellationToken cancellationToken = default);
}
