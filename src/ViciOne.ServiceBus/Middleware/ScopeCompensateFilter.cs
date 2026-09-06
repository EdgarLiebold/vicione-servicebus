using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes scope compensate pipeline stages.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public class ScopeCompensateFilter<TLog> :
    IFilter<CompensateContext<TLog>>
    where TLog : class
{
    readonly CompensateScopeProvider<TLog> _scopeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scopeProvider">The scope provider.</param>
    public ScopeCompensateFilter(CompensateScopeProvider<TLog> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(CompensateContext<TLog> context, IPipe<CompensateContext<TLog>> next)
    {
        await using ICompensateScopeContext<TLog> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scope");
    }
}
