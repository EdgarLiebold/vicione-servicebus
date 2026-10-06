using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes inline pipeline stages.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class InlineFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly InlineFilterMethod<TContext> _filterMethod;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filterMethod">The filter method.</param>
    public InlineFilter(InlineFilterMethod<TContext> filterMethod)
    {
        _filterMethod = filterMethod;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("inline");
    }

    /// <summary>Delegates context handling and continuation policy to the supplied filter callback.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    public Task SendAsync(TContext context, IPipe<TContext> next)
    {
        return _filterMethod(context, next);
    }
}
