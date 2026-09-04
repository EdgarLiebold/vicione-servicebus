using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class ReceiveTransportObservable :
    Connectable<IReceiveTransportObserver>,
    IReceiveTransportObserver
{
    public Task Ready(ReceiveTransportReady ready)
    {
        return ForEachAsync(x => x.Ready(ready));
    }

    public Task Completed(ReceiveTransportCompleted completed)
    {
        return ForEachAsync(x => x.Completed(completed));
    }

    public Task Faulted(ReceiveTransportFaulted faulted)
    {
        return ForEachAsync(x => x.Faulted(faulted));
    }
}
