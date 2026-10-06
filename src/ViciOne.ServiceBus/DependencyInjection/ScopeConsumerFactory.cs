using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumers;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Creates scope consumer instances.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class ScopeConsumerFactory<TConsumer> :
    IConsumerFactory<TConsumer>
    where TConsumer : class
{
    readonly IConsumeScopeProvider _scopeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scopeProvider">The scope provider.</param>
    public ScopeConsumerFactory(IConsumeScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    }

    /// <summary>Acquires a consumer scope, awaits the next consumer stage, and releases the acquired scope.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IConsumerConsumeScopeContext<TConsumer, TMessage> scope = await _scopeProvider.GetScopeAsync<TConsumer, TMessage>(context);
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

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateConsumerFactoryScope<TConsumer>("scope");
        _scopeProvider.Probe(scope);
    }
}
