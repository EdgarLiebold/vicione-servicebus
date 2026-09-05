using System.Threading.Tasks;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Sends the message through the outbox
/// </summary>
/// <typeparam name="TContext">The outbox context type</typeparam>
/// <typeparam name="TMessage">The message type</typeparam>
public class OutboxConsumeFilter<TContext, TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TContext : class
    where TMessage : class
{
    readonly OutboxConsumeOptions _options;
    readonly IConsumeScopeProvider _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scopeProvider">The scope provider value.</param>
    /// <param name="options">The options value.</param>
    public OutboxConsumeFilter(IConsumeScopeProvider scopeProvider, OutboxConsumeOptions options)
    {
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("outbox");
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        await using IConsumeScopeContext<TMessage> scope = await _scopeProvider.GetScopeAsync(context).ConfigureAwait(false);

        var contextFactory = scope.GetService<IOutboxContextFactory<TContext>>();
        if (contextFactory == null)
            throw new ConsumerException($"Unable to resolve outbox context factory for type '{TypeCache<TContext>.ShortName}'.");

        var pipe = new OutboxMessagePipe<TMessage>(_options, scope, next);

        await contextFactory.SendAsync(scope.Context, _options, pipe).ConfigureAwait(false);
    }
}
