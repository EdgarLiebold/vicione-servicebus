using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes scope consume pipeline stages.</summary>
public class ScopeConsumeFilter :
    IFilter<ConsumeContext>
{
    readonly IConsumeScopeProvider _scopeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scopeProvider">The scope provider.</param>
    public ScopeConsumeFilter(IConsumeScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>Runs the continuation in an acquired consume scope and releases the owned scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        var scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

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
