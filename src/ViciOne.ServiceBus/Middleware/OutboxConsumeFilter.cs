using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Sends the message through the outbox.</summary>
/// <typeparam name="TContext">The outbox context type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public class OutboxConsumeFilter<TContext, TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TContext : class
    where TMessage : class
{
    readonly OutboxConsumeOptions _options;
    readonly IConsumeScopeProvider _scopeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scopeProvider">The scope provider.</param>
    /// <param name="options">The options that control the operation.</param>
    public OutboxConsumeFilter(IConsumeScopeProvider scopeProvider, OutboxConsumeOptions options)
    {
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("outbox");
    }

    /// <summary>Runs the supplied consume pipeline through the scoped outbox context factory.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        IConsumeScopeContext<TMessage> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        Exception? operationFailure = null;
        try
        {
            var contextFactory = scope.GetService<IOutboxContextFactory<TContext>>();
            if (contextFactory == null)
                throw new ConsumerException($"Unable to resolve outbox context factory for type '{TypeCache<TContext>.ShortName}'.");

            var pipe = new OutboxMessagePipe<TMessage>(_options, scope, next);

            await contextFactory.SendAsync(scope.Context, _options, pipe).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }

        await OwnedConsumerLifetime.ReleaseAfterOperationAsync(scope, operationFailure).ConfigureAwait(false);
    }
}
