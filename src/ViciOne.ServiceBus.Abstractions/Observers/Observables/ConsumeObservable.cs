using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class ConsumeObservable :
    Connectable<IConsumeObserver>,
    IConsumeObserver
{
    public Task PreConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PreConsumeAsync(context));
    }

    public Task PostConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PostConsumeAsync(context));
    }

    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.ConsumeFaultAsync(context, exception));
    }
}
