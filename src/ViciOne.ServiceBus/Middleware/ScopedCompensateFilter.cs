using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumers;
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

    /// <summary>Resolves and runs the configured filter in an acquired compensate scope, then releases the owned scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(CompensateContext<TArguments> context, IPipe<CompensateContext<TArguments>> next)
    {
        ICompensateScopeContext<TArguments> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        Exception? operationFailure = null;
        try
        {
            var filter = scope.GetService<TFilter>();
            await filter.SendAsync(scope.Context, next).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }

        await OwnedConsumerLifetime.ReleaseAfterOperationAsync(scope, operationFailure).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("scopedFilter");
        scope.Add("filter", TypeCache<TFilter>.ShortName);

        _scopeProvider.Probe(scope);
    }
}
