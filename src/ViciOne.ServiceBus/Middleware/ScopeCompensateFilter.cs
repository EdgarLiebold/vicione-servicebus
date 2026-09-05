using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a scope compensate filter implementation.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public class ScopeCompensateFilter<TLog> :
    IFilter<CompensateContext<TLog>>
    where TLog : class
{
    readonly CompensateScopeProvider<TLog> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scopeProvider">The scope provider value.</param>
    public ScopeCompensateFilter(CompensateScopeProvider<TLog> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(CompensateContext<TLog> context, IPipe<CompensateContext<TLog>> next)
    {
        await using ICompensateScopeContext<TLog> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scope");
    }
}
