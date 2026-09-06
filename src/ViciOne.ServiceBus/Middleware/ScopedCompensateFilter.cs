using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes scoped compensate pipeline stages.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TFilter">The filter type.</typeparam>
public class ScopedCompensateFilter<TArguments, TFilter> :
    IFilter<CompensateContext<TArguments>>
    where TArguments : class
    where TFilter : class, IFilter<CompensateContext<TArguments>>
{
    readonly CompensateScopeProvider<TArguments> _scopeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scopeProvider">The scope provider.</param>
    public ScopedCompensateFilter(CompensateScopeProvider<TArguments> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(CompensateContext<TArguments> context, IPipe<CompensateContext<TArguments>> next)
    {
        await using ICompensateScopeContext<TArguments> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        var filter = scope.GetService<TFilter>();

        await filter.SendAsync(scope.Context, next).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("scopedFilter");
        scope.Add("filter", TypeMetadataCache<TFilter>.ShortName);

        _scopeProvider.Probe(scope);
    }
}
