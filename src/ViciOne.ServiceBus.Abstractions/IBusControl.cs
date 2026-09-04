using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// Defines the contract for bus control.
/// </summary>
public interface IBusControl :
    IBus
{
    /// <summary>
    /// Starts the bus (assuming the battery isn't dead). Once the bus has been started it cannot be started again, even after it has been stopped.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the bus and all receive endpoints are ready.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the bus if it has been started. If the bus hasn't been started, the method returns without any warning.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the health of the bus, including all receive endpoints
    /// </summary>
    /// <returns></returns>
    BusHealthResult CheckHealth();
}
