using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes observations for receive endpoint.</summary>
public class ReceiveEndpointObservable :
    Connectable<IReceiveEndpointObserver>,
    IReceiveEndpointObserver
{
    /// <summary>Reports that the component is ready.</summary>
    /// <param name="ready">The ready.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ReadyAsync(ReceiveEndpointReady ready)
    {
        return ForEachAsync(x => x.ReadyAsync(ready));
    }

    /// <summary>Stops ping.</summary>
    /// <param name="stopping">The stopping.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StoppingAsync(ReceiveEndpointStopping stopping)
    {
        return ForEachAsync(x => x.StoppingAsync(stopping));
    }

    /// <summary>Reports successful completion.</summary>
    /// <param name="completed">The completed.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CompletedAsync(ReceiveEndpointCompleted completed)
    {
        return ForEachAsync(x => x.CompletedAsync(completed));
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <param name="faulted">The faulted.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync(ReceiveEndpointFaulted faulted)
    {
        return ForEachAsync(x => x.FaultedAsync(faulted));
    }
}
