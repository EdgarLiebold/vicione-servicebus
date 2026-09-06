using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>A handle to an active transport.</summary>
public interface ReceiveTransportHandle
{
    /// <summary>Stop the transport, releasing any resources associated with the endpoint.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
