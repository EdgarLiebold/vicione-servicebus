using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for host rider handle.
/// </summary>
public interface HostRiderHandle
{
    /// <summary>
    /// Gets the rider value.
    /// </summary>
    IRider Rider { get; }

    /// <summary>
    /// Gets the ready value.
    /// </summary>
    Task<RiderReady> Ready { get; }

    /// <summary>
    /// Stops the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the configured component.
    /// </summary>
    /// <param name="remove">The remove value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task StopAsync(bool remove, CancellationToken cancellationToken = default);
}
