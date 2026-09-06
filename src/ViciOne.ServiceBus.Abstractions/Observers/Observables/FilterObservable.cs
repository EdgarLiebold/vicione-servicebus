using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes observations for filter.</summary>
public class FilterObservable :
    Connectable<IFilterObserver>,
    IFilterObserver
{
    /// <summary>Runs before send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreSendAsync<T>(T context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PreSendAsync(context));
    }

    /// <summary>Runs after send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostSendAsync<T>(T context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PostSendAsync(context));
    }

    /// <summary>Sends fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendFaultAsync<T>(T context, Exception exception)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.SendFaultAsync(context, exception));
    }
}


/// <summary>Publishes observations for filter.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class FilterObservable<TContext> :
    Connectable<IFilterObserver<TContext>>,
    IFilterObserver<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Runs before send.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreSendAsync(TContext context)
    {
        return ForEachAsync(x => x.PreSendAsync(context));
    }

    /// <summary>Runs after send.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostSendAsync(TContext context)
    {
        return ForEachAsync(x => x.PostSendAsync(context));
    }

    /// <summary>Sends fault.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendFaultAsync(TContext context, Exception exception)
    {
        return ForEachAsync(x => x.SendFaultAsync(context, exception));
    }
}
