using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes observations for receive transport.</summary>
public class ReceiveTransportObservable :
    Connectable<IReceiveTransportObserver>,
    IReceiveTransportObserver
{
    /// <summary>Reports that the component is ready.</summary>
    /// <param name="ready">The ready.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ReadyAsync(ReceiveTransportReady ready)
    {
        return ForEachAsync(x => x.ReadyAsync(ready));
    }

    /// <summary>Reports successful completion.</summary>
    /// <param name="completed">The completed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CompletedAsync(ReceiveTransportCompleted completed)
    {
        return ForEachAsync(x => x.CompletedAsync(completed));
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <param name="faulted">The faulted.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync(ReceiveTransportFaulted faulted)
    {
        return ForEachAsync(x => x.FaultedAsync(faulted));
    }
}
