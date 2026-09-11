using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Controls the lifetime of one active receive transport generation.</summary>
public interface ReceiveTransportHandle
{
    /// <summary>Stops the transport and releases the resources owned by its generation.</summary>
    /// <param name="cancellationToken">The token that cancels shutdown.</param>
    /// <returns>A task that completes after the transport has stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}
