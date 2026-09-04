using System.Collections.Generic;
using System.Threading;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for rider control.
/// </summary>
public interface IRiderControl :
    IRider
{
    /// <summary>
    /// Starts the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    RiderHandle Start(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the check endpoint health operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IEnumerable<EndpointHealthResult> CheckEndpointHealth();
}
