using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Resolves and observes producers for Event Hubs endpoint addresses.</summary>
public interface IEventHubProducerProvider :
    ISendObserverConnector
{
    /// <summary>Gets a producer for the specified Event Hubs endpoint.</summary>
    /// <param name="address">The Event Hubs endpoint address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the endpoint producer.</returns>
    Task<IEventHubProducer> GetProducerAsync(Uri address, CancellationToken cancellationToken = default);
}
