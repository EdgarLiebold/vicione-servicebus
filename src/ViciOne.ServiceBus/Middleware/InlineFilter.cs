using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides an inline filter implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class InlineFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly InlineFilterMethod<TContext> _filterMethod;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filterMethod">The filter method value.</param>
    public InlineFilter(InlineFilterMethod<TContext> filterMethod)
    {
        _filterMethod = filterMethod;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("inline");
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
        return _filterMethod(context, next);
    }
}
