using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class SendObservable :
    Connectable<ISendObserver>,
    ISendObserver
{
    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PreSendAsync(context));
    }

    public Task PostSendAsync<T>(SendContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PostSendAsync(context));
    }

    public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.SendFaultAsync(context, exception));
    }
}
