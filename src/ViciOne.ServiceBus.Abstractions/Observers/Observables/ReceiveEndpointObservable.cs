using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class ReceiveEndpointObservable :
    Connectable<IReceiveEndpointObserver>,
    IReceiveEndpointObserver
{
    public Task ReadyAsync(ReceiveEndpointReady ready)
    {
        return ForEachAsync(x => x.ReadyAsync(ready));
    }

    public Task StoppingAsync(ReceiveEndpointStopping stopping)
    {
        return ForEachAsync(x => x.StoppingAsync(stopping));
    }

    public Task CompletedAsync(ReceiveEndpointCompleted completed)
    {
        return ForEachAsync(x => x.CompletedAsync(completed));
    }

    public Task FaultedAsync(ReceiveEndpointFaulted faulted)
    {
        return ForEachAsync(x => x.FaultedAsync(faulted));
    }
}
