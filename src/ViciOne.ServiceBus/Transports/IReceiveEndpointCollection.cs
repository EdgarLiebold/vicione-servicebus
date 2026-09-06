using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by receive endpoint collection.</summary>
public interface IReceiveEndpointCollection :
    IReceiveEndpointObserverConnector,
    IConsumeMessageObserverConnector,
    IProbeSite
{
    /// <summary>Add an endpoint to the collection.</summary>
    /// <param name="endpointName">The endpoint name.</param>
    /// <param name="endpoint">The endpoint.</param>
    void Add(string endpointName, ReceiveEndpoint endpoint);

    /// <summary>
    /// Start all endpoints in the collection which have not been started, and return the handles
    /// for those endpoints.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The host receive endpoint handle array produced by the operation.</returns>
    HostReceiveEndpointHandle[] StartEndpoints(CancellationToken cancellationToken);

    /// <summary>Start a new receive endpoint.</summary>
    /// <param name="endpointName">The endpoint name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The host receive endpoint handle produced by the operation.</returns>
    HostReceiveEndpointHandle Start(string endpointName, CancellationToken cancellationToken = default);

    /// <summary>Stop all receive endpoints.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopEndpointsAsync(CancellationToken cancellationToken);

    /// <summary>Checks endpoint health.</summary>
    /// <returns>The enumerable produced by the operation.</returns>
    IEnumerable<EndpointHealthResult> CheckEndpointHealth();
}
