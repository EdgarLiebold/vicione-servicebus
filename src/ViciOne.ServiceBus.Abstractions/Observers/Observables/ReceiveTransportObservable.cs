using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class ReceiveTransportObservable :
    Connectable<IReceiveTransportObserver>,
    IReceiveTransportObserver
{
    public Task ReadyAsync(ReceiveTransportReady ready)
    {
        return ForEachAsync(x => x.ReadyAsync(ready));
    }

    public Task CompletedAsync(ReceiveTransportCompleted completed)
    {
        return ForEachAsync(x => x.CompletedAsync(completed));
    }

    public Task FaultedAsync(ReceiveTransportFaulted faulted)
    {
        return ForEachAsync(x => x.FaultedAsync(faulted));
    }
}
