using System.Collections.Generic;
using System.Threading;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by rider control.</summary>
public interface IRiderControl :
    IRider
{
    /// <summary>Starts the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The rider handle produced by the operation.</returns>
    RiderHandle Start(CancellationToken cancellationToken = default);

    /// <summary>Checks endpoint health.</summary>
    /// <returns>The enumerable produced by the operation.</returns>
    IEnumerable<EndpointHealthResult> CheckEndpointHealth();
}
