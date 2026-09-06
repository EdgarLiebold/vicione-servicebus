using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for host handle.
/// </summary>
public interface HostHandle
{
    /// <summary>
    /// A task which can be awaited to know when the host is ready
    /// </summary>
    Task<HostReady> Ready { get; }

    /// <summary>
    /// Stops the host and releases its transport resources.
    /// </summary>
    /// <returns>A task that completes when the host has stopped.</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task StopAsync(CancellationToken cancellationToken = default);
}
