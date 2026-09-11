using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Owns receive-endpoint registration, lifecycle, health, observation, and diagnostics for a host or rider.</summary>
public interface IReceiveEndpointCollection :
    IReceiveEndpointObserverConnector,
    IConsumeMessageObserverConnector,
    IProbeSite
{
    /// <summary>Adds a configured endpoint under its host-unique name.</summary>
    /// <param name="endpointName">The name that identifies the endpoint within the collection.</param>
    /// <param name="endpoint">The endpoint owned by the collection.</param>
    void Add(string endpointName, ReceiveEndpoint endpoint);

    /// <summary>Starts every endpoint that is not already running.</summary>
    /// <param name="cancellationToken">The token that cancels endpoint startup.</param>
    /// <returns>The handles for the endpoint generations started by this call.</returns>
    IHostReceiveEndpointHandle[] StartEndpoints(CancellationToken cancellationToken);

    /// <summary>Starts one registered receive endpoint.</summary>
    /// <param name="endpointName">The name of the endpoint to start.</param>
    /// <param name="cancellationToken">The token that cancels endpoint startup.</param>
    /// <returns>A handle that exposes readiness and controls the connected endpoint.</returns>
    IHostReceiveEndpointHandle Start(string endpointName, CancellationToken cancellationToken = default);

    /// <summary>Stops every active endpoint while retaining its registration for a later host restart.</summary>
    /// <param name="cancellationToken">The token that cancels endpoint shutdown.</param>
    /// <returns>A task that completes after every endpoint has stopped.</returns>
    Task StopEndpointsAsync(CancellationToken cancellationToken);

    /// <summary>Gets a health snapshot for every registered endpoint.</summary>
    /// <returns>The current endpoint health results.</returns>
    IEnumerable<EndpointHealthResult> CheckEndpointHealth();
}
