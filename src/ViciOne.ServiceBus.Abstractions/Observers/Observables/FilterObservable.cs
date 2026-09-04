using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class FilterObservable :
    Connectable<IFilterObserver>,
    IFilterObserver
{
    public Task PreSendAsync<T>(T context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PreSendAsync(context));
    }

    public Task PostSendAsync<T>(T context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PostSendAsync(context));
    }

    public Task SendFaultAsync<T>(T context, Exception exception)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.SendFaultAsync(context, exception));
    }
}


public class FilterObservable<TContext> :
    Connectable<IFilterObserver<TContext>>,
    IFilterObserver<TContext>
    where TContext : class, PipeContext
{
    public Task PreSendAsync(TContext context)
    {
        return ForEachAsync(x => x.PreSendAsync(context));
    }

    public Task PostSendAsync(TContext context)
    {
        return ForEachAsync(x => x.PostSendAsync(context));
    }

    public Task SendFaultAsync(TContext context, Exception exception)
    {
        return ForEachAsync(x => x.SendFaultAsync(context, exception));
    }
}
