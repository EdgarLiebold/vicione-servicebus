using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// A content filter applies a delegate to the message context, and uses the result to either accept the message
/// or discard it.
/// </summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ContextFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly Func<TContext, Task<bool>> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public ContextFilter(Func<TContext, Task<bool>> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    /// <summary>Invokes the continuation only when the configured asynchronous predicate accepts the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        Task<bool> filterTask = _filter(context)
            ?? throw new InvalidOperationException("The context filter returned a null decision task.");
        if (filterTask.Status == TaskStatus.RanToCompletion && filterTask.Result)
            return next.SendAsync(context);

        async Task SendAsync()
        {
            var accept = await filterTask.ConfigureAwait(false);
            if (accept)
                await next.SendAsync(context).ConfigureAwait(false);
        }

        return SendAsync();
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateFilterScope("context");
    }
}
