using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a scoped consume filter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <typeparam name="TFilter">The t filter type.</typeparam>
public class ScopedConsumeFilter<T, TFilter> :
    IFilter<ConsumeContext<T>>
    where T : class
    where TFilter : class, IFilter<ConsumeContext<T>>
{
    readonly IConsumeScopeProvider _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scopeProvider">The scope provider value.</param>
    public ScopedConsumeFilter(IConsumeScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        await using IConsumeScopeContext<T> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

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
