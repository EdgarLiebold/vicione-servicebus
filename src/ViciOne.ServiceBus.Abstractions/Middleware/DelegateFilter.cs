using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes delegate pipeline stages.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class DelegateFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly Action<TContext> _callback;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    public DelegateFilter(Action<TContext> callback)
    {
        _callback = callback;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("delegate");
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        _callback(context);

        return next.SendAsync(context);
    }
}
