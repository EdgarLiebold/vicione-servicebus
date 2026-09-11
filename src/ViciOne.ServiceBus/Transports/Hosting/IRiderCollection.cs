using System.Collections.Generic;
using System.Threading;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines registration, lifecycle, and health operations for named riders.</summary>
public interface IRiderCollection :
    IAgent
{
    /// <summary>Gets a rider from the active lifecycle generation.</summary>
    /// <param name="name">The rider registration name.</param>
    /// <returns>The active rider.</returns>
    IRider Get(string name);

    /// <summary>Adds a rider registration.</summary>
    /// <param name="name">The unique rider registration name.</param>
    /// <param name="rider">The rider lifecycle controller.</param>
    void Add(string name, IRiderControl rider);

    /// <summary>Starts every registered rider that has no active generation.</summary>
    /// <param name="cancellationToken">The token that cancels rider startup.</param>
    /// <returns>The handles created for this start operation.</returns>
    HostRiderHandle[] StartRiders(CancellationToken cancellationToken = default);

    /// <summary>Starts a named rider that has no active generation.</summary>
    /// <param name="name">The rider registration name.</param>
    /// <param name="cancellationToken">The token that cancels rider startup.</param>
    /// <returns>The handle for the new rider generation.</returns>
    HostRiderHandle StartRider(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets health observations from every registered rider.</summary>
    /// <returns>A stable snapshot of rider endpoint health results.</returns>
    IEnumerable<EndpointHealthResult> CheckEndpointHealth();
}
