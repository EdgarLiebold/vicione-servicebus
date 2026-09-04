using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class ReceiveObservable :
    Connectable<IReceiveObserver>,
    IReceiveObserver
{
    public Task PreReceiveAsync(ReceiveContext context)
    {
        return ForEachAsync(x => x.PreReceiveAsync(context));
    }

    public Task PostReceiveAsync(ReceiveContext context)
    {
        return ForEachAsync(x => x.PostReceiveAsync(context));
    }

    public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        return ForEachAsync(x => x.PostConsumeAsync(context, duration, consumerType));
    }

    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.ConsumeFaultAsync(context, duration, consumerType, exception));
    }

    public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
    {
        return ForEachAsync(x => x.ReceiveFaultAsync(context, exception));
    }
}
