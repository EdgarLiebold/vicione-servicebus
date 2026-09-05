using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a scoped compensate filter implementation.
/// </summary>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
/// <typeparam name="TFilter">The t filter type.</typeparam>
public class ScopedCompensateFilter<TArguments, TFilter> :
    IFilter<CompensateContext<TArguments>>
    where TArguments : class
    where TFilter : class, IFilter<CompensateContext<TArguments>>
{
    readonly CompensateScopeProvider<TArguments> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scopeProvider">The scope provider value.</param>
    public ScopedCompensateFilter(CompensateScopeProvider<TArguments> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(CompensateContext<TArguments> context, IPipe<CompensateContext<TArguments>> next)
    {
        await using ICompensateScopeContext<TArguments> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        var filter = scope.GetService<TFilter>();

        await filter.SendAsync(scope.Context, next).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("scopedFilter");
        scope.Add("filter", TypeMetadataCache<TFilter>.ShortName);

        _scopeProvider.Probe(scope);
    }
}
