using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Resolves address-specific send endpoints while preserving the provider's message context.</summary>
public interface ISendEndpointProvider :
    ISendObserverConnector
{
    /// <summary>Gets the send endpoint for an address.</summary>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A non-null task containing the non-null resolved send endpoint.</returns>
    Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default);
}
