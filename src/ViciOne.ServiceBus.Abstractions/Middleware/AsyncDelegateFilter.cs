using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes async delegate pipeline stages.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class AsyncDelegateFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly Func<TContext, Task> _callback;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public AsyncDelegateFilter(Func<TContext, Task> callback)
    {
        _callback = callback;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("asyncDelegate");
    }

    /// <summary>Invokes the asynchronous callback and, after it succeeds, invokes the continuation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
