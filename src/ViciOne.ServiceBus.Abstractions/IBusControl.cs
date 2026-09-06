using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Defines the operations required by bus control.</summary>
public interface IBusControl :
    IBus
{
    /// <summary>Starts the bus and waits until its receive endpoints are ready.</summary>
    /// <param name="cancellationToken">The token that cancels startup and its readiness wait.</param>
    /// <returns>A task that completes when the bus and all receive endpoints are ready.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the bus if it is running; otherwise, completes without changing state.</summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the current health of the bus and its receive endpoints.</summary>
    /// <returns>The current aggregate bus health.</returns>
    BusHealthResult CheckHealth();
}
