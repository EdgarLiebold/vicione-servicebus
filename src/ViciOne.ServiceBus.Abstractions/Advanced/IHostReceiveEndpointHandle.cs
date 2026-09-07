using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes a host-connected receive endpoint and controls its lifetime.</summary>
public interface IHostReceiveEndpointHandle
{
    /// <summary>Gets the connected endpoint used for message operations and observation.</summary>
    IReceiveEndpoint ReceiveEndpoint { get; }

    /// <summary>Gets a task that completes when the endpoint is ready to consume messages.</summary>
    Task<ReceiveEndpointReady> Ready { get; }

    /// <summary>
    /// Stops the receive endpoint and removes it from the host. Once removed, the endpoint
    /// cannot be restarted using the <see cref="ReceiveEndpoint"/> property directly.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>A task that completes after the endpoint has stopped and has been removed from the host.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
