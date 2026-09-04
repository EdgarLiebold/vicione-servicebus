using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for receive endpoint collection.
/// </summary>
public interface IReceiveEndpointCollection :
    IReceiveEndpointObserverConnector,
    IConsumeMessageObserverConnector,
    IProbeSite
{
    /// <summary>
    /// Add an endpoint to the collection
    /// </summary>
    /// <param name="endpointName"></param>
    /// <param name="endpoint"></param>
    void Add(string endpointName, ReceiveEndpoint endpoint);

    /// <summary>
    /// Start all endpoints in the collection which have not been started, and return the handles
    /// for those endpoints.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    HostReceiveEndpointHandle[] StartEndpoints(CancellationToken cancellationToken);

    /// <summary>
    /// Start a new receive endpoint
    /// </summary>
    /// <param name="endpointName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    HostReceiveEndpointHandle Start(string endpointName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop all receive endpoints
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StopEndpointsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Performs the check endpoint health operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IEnumerable<EndpointHealthResult> CheckEndpointHealth();
}
