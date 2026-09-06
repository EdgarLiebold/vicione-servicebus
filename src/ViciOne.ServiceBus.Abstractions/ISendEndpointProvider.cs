using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// The Send Endpoint Provider is used to retrieve endpoints using addresses. The interface is
/// available both at the bus and within the context of most message receive handlers, including
/// the consume context, saga context, consumer context, etc. The most local provider should be
/// used to ensure message continuity is maintained.
/// </summary>
public interface ISendEndpointProvider :
    ISendObserverConnector
{
    /// <summary>Return the send endpoint for the specified address.</summary>
    /// <param name="address">The endpoint address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The send endpoint.</returns>
    Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default);
}
