using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for rider handle.
/// </summary>
public interface RiderHandle
{
    /// <summary>
    /// Gets the ready value.
    /// </summary>
    Task Ready { get; }
    /// <summary>
    /// Stops the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
