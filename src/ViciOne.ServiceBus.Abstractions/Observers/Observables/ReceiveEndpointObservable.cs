using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a receive endpoint observable implementation.
/// </summary>
public class ReceiveEndpointObservable :
    Connectable<IReceiveEndpointObserver>,
    IReceiveEndpointObserver
{
    /// <summary>
    /// Performs the ready operation.
    /// </summary>
    /// <param name="ready">The ready value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ReadyAsync(ReceiveEndpointReady ready)
    {
        return ForEachAsync(x => x.ReadyAsync(ready));
    }

    /// <summary>
    /// Stops ping.
    /// </summary>
    /// <param name="stopping">The stopping value.</param>
    /// <returns>The result of the operation.</returns>
    public Task StoppingAsync(ReceiveEndpointStopping stopping)
    {
        return ForEachAsync(x => x.StoppingAsync(stopping));
    }

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <param name="completed">The completed value.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompletedAsync(ReceiveEndpointCompleted completed)
    {
        return ForEachAsync(x => x.CompletedAsync(completed));
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="faulted">The faulted value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync(ReceiveEndpointFaulted faulted)
    {
        return ForEachAsync(x => x.FaultedAsync(faulted));
    }
}
