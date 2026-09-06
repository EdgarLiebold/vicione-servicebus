using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>A handle to an active endpoint.</summary>
public interface ReceiveEndpointHandle
{
    /// <summary>A task which can be awaited to know when the receive endpoint is ready.</summary>
    Task<ReceiveEndpointReady> Ready { get; }

    /// <summary>Stop the endpoint, releasing any resources associated with the endpoint.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
