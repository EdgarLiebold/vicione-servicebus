using System.Collections.Generic;
using System.Threading;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for rider collection.
/// </summary>
public interface IRiderCollection :
    IAgent
{
    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the operation.</returns>
    IRider Get(string name);

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="rider">The rider value.</param>
    void Add(string name, IRiderControl rider);

    /// <summary>
    /// Starts riders.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    HostRiderHandle[] StartRiders(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts rider.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    HostRiderHandle StartRider(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the check endpoint health operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IEnumerable<EndpointHealthResult> CheckEndpointHealth();
}
