using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides an async delegate filter implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class AsyncDelegateFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly Func<TContext, Task> _callback;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public AsyncDelegateFilter(Func<TContext, Task> callback)
    {
        _callback = callback;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("asyncDelegate");
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        var callbackTask = _callback(context);
        if (callbackTask.Status == TaskStatus.RanToCompletion)
            return next.SendAsync(context);

        async Task SendAsync()
        {
            await callbackTask.ConfigureAwait(false);

            await next.SendAsync(context).ConfigureAwait(false);
        }

        return SendAsync();
    }
}
