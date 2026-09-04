using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a scoped filter implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class ScopedFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IFilterScopeProvider<TContext> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scopeProvider">The scope provider value.</param>
    public ScopedFilter(IFilterScopeProvider<TContext> scopeProvider)
    {
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        await using IFilterScopeContext<TContext> scope = _scopeProvider.Create(context);

        await scope.Filter.SendAsync(scope.Context, next).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("scopedFilter");

        _scopeProvider.Probe(scope);
    }
}
