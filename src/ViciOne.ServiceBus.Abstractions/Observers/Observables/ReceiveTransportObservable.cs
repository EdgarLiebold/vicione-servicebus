using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a receive transport observable implementation.
/// </summary>
public class ReceiveTransportObservable :
    Connectable<IReceiveTransportObserver>,
    IReceiveTransportObserver
{
    /// <summary>
    /// Performs the ready operation.
    /// </summary>
    /// <param name="ready">The ready value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ReadyAsync(ReceiveTransportReady ready)
    {
        return ForEachAsync(x => x.ReadyAsync(ready));
    }

    /// <summary>
    /// Performs the completed operation.
    /// </summary>
    /// <param name="completed">The completed value.</param>
    /// <returns>The result of the operation.</returns>
    public Task CompletedAsync(ReceiveTransportCompleted completed)
    {
        return ForEachAsync(x => x.CompletedAsync(completed));
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <param name="faulted">The faulted value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync(ReceiveTransportFaulted faulted)
    {
        return ForEachAsync(x => x.FaultedAsync(faulted));
    }
}
