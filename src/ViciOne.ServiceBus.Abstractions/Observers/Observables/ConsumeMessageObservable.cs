using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class ConsumeMessageObservable<T> :
    Connectable<IConsumeMessageObserver<T>>,
    IConsumeMessageObserver<T>
    where T : class
{
    public Task PreConsumeAsync(ConsumeContext<T> context)
    {
        return ForEachAsync(x => x.PreConsumeAsync(context));
    }

    public Task PostConsumeAsync(ConsumeContext<T> context)
    {
        return ForEachAsync(x => x.PostConsumeAsync(context));
    }

    public Task ConsumeFaultAsync(ConsumeContext<T> context, Exception exception)
    {
        return ForEachAsync(x => x.ConsumeFaultAsync(context, exception));
    }
}
