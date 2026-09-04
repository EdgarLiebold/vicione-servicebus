using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a filter pipe implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class FilterPipe<TContext> :
    IPipe<TContext>
    where TContext : class, PipeContext
{
    readonly IFilter<TContext> _filter;
    readonly IPipe<TContext> _next;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="next">The next value.</param>
    public FilterPipe(IFilter<TContext> filter, IPipe<TContext> next)
    {
        _filter = filter;
        _next = next;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _filter.Probe(context);
        _next.Probe(context);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    [DebuggerStepThrough]
    public Task SendAsync(TContext context)
    {
        return _filter.SendAsync(context, _next);
    }
}
