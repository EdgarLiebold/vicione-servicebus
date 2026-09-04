using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a delegate filter implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class DelegateFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly Action<TContext> _callback;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public DelegateFilter(Action<TContext> callback)
    {
        _callback = callback;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("delegate");
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
        _callback(context);

        return next.SendAsync(context);
    }
}
