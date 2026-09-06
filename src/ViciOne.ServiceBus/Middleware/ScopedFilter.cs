using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes scoped pipeline stages.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ScopedFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IFilterScopeProvider<TContext> _scopeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scopeProvider">The scope provider.</param>
    public ScopedFilter(IFilterScopeProvider<TContext> scopeProvider)
    {
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        await using IFilterScopeContext<TContext> scope = _scopeProvider.Create(context);

        await scope.Filter.SendAsync(scope.Context, next).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("scopedFilter");

        _scopeProvider.Probe(scope);
    }
}
