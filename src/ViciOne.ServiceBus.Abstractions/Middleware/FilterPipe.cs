using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for filter.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class FilterPipe<TContext> :
    IPipe<TContext>
    where TContext : class, PipeContext
{
    readonly IFilter<TContext> _filter;
    readonly IPipe<TContext> _next;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    public FilterPipe(IFilter<TContext> filter, IPipe<TContext> next)
    {
        _filter = filter;
        _next = next;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        _filter.Probe(context);
        _next.Probe(context);
    }

    /// <summary>Invokes the retained filter with its configured continuation and returns its task.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerStepThrough]
    public Task SendAsync(TContext context)
    {
        return _filter.SendAsync(context, _next);
    }
}
