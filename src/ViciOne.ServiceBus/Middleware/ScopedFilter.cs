using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumers;
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

    /// <summary>Runs the acquired scope's filter and releases the owned scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(TContext context, IPipe<TContext> next)
    {
        IFilterScopeContext<TContext> scope = _scopeProvider.Create(context);

        Exception? operationFailure = null;
        try
        {
            await scope.Filter.SendAsync(scope.Context, next).ConfigureAwait(false);
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

        _scopeProvider.Probe(scope);
    }
}
