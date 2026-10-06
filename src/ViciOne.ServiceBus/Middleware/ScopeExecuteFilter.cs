using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes scope execute pipeline stages.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class ScopeExecuteFilter<TArguments> :
    IFilter<ExecuteContext<TArguments>>
    where TArguments : class
{
    readonly ExecuteScopeProvider<TArguments> _scopeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scopeProvider">The scope provider.</param>
    public ScopeExecuteFilter(ExecuteScopeProvider<TArguments> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>Runs the continuation in an acquired execute scope and releases the owned scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ExecuteContext<TArguments> context, IPipe<ExecuteContext<TArguments>> next)
    {
        IExecuteScopeContext<TArguments> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        Exception? operationFailure = null;
        try
        {
            await next.SendAsync(scope.Context).ConfigureAwait(false);
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
        context.CreateFilterScope("scope");
    }
}
