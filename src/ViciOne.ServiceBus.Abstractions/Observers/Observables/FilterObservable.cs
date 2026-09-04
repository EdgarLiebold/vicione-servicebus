using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a filter observable implementation.
/// </summary>
public class FilterObservable :
    Connectable<IFilterObserver>,
    IFilterObserver
{
    /// <summary>
    /// Performs the pre send operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreSendAsync<T>(T context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PreSendAsync(context));
    }

    /// <summary>
    /// Performs the post send operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostSendAsync<T>(T context)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.PostSendAsync(context));
    }

    /// <summary>
    /// Sends fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendFaultAsync<T>(T context, Exception exception)
        where T : class, PipeContext
    {
        return ForEachAsync(x => x.SendFaultAsync(context, exception));
    }
}


/// <summary>
/// Provides a filter observable implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class FilterObservable<TContext> :
    Connectable<IFilterObserver<TContext>>,
    IFilterObserver<TContext>
    where TContext : class, PipeContext
{
    /// <summary>
    /// Performs the pre send operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreSendAsync(TContext context)
    {
        return ForEachAsync(x => x.PreSendAsync(context));
    }

    /// <summary>
    /// Performs the post send operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostSendAsync(TContext context)
    {
        return ForEachAsync(x => x.PostSendAsync(context));
    }

    /// <summary>
    /// Sends fault.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendFaultAsync(TContext context, Exception exception)
    {
        return ForEachAsync(x => x.SendFaultAsync(context, exception));
    }
}
