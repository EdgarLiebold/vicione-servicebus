using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Controls the lifetime of host rider.</summary>
public interface HostRiderHandle
{
    /// <summary>Gets the rider.</summary>
    IRider Rider { get; }

    /// <summary>Gets the ready.</summary>
    Task<RiderReady> Ready { get; }

    /// <summary>Stops the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the configured component.</summary>
    /// <param name="remove">The remove.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopAsync(bool remove, CancellationToken cancellationToken = default);
}
