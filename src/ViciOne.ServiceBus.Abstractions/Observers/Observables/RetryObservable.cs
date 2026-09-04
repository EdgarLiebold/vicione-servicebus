using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class RetryObservable :
    Connectable<IRetryObserver>,
    IRetryObserver
{
    public Task PostCreateAsync<T>(RetryPolicyContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PostCreateAsync(context));
    }

    public Task PostFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PostFaultAsync(context));
    }

    public Task PreRetryAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PreRetryAsync(context));
    }

    public Task RetryFaultAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.RetryFaultAsync(context));
    }

    public Task RetryCompleteAsync<T>(RetryContext<T> context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.RetryCompleteAsync(context));
    }

    public Task RetryFaultAsync(RetryContext context, CancellationToken cancellationToken = default)
    {
        return ForEachAsync(x => RetryFaultObserverCache.RetryFaultAsync(x, context, context.ContextType, cancellationToken: cancellationToken), cancellationToken: cancellationToken);
    }
}
