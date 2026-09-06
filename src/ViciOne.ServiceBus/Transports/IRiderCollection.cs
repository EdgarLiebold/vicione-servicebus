using System.Collections.Generic;
using System.Threading;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by rider collection.</summary>
public interface IRiderCollection :
    IAgent
{
    /// <summary>Retrieves the requested value.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The requested value.</returns>
    IRider Get(string name);

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="name">The name.</param>
    /// <param name="rider">The rider.</param>
    void Add(string name, IRiderControl rider);

    /// <summary>Starts riders.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The host rider handle array produced by the operation.</returns>
    HostRiderHandle[] StartRiders(CancellationToken cancellationToken = default);

    /// <summary>Starts rider.</summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The host rider handle produced by the operation.</returns>
    HostRiderHandle StartRider(string name, CancellationToken cancellationToken = default);

    /// <summary>Checks endpoint health.</summary>
    /// <returns>The enumerable produced by the operation.</returns>
    IEnumerable<EndpointHealthResult> CheckEndpointHealth();
}
