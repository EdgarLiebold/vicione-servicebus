using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Redirects send and publish endpoint resolution through an in-memory outbox.</summary>
internal sealed class InMemoryOutboxReceiveContext :
    ReceiveContextProxy
{
    /// <summary>Initializes the decorator over an existing receive context.</summary>
    /// <param name="outboxContext">The outbox that defers outgoing operations.</param>
    /// <param name="context">The receive context whose endpoint providers are decorated.</param>
    public InMemoryOutboxReceiveContext(OutboxContext outboxContext, ReceiveContext context)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        ArgumentNullException.ThrowIfNull(outboxContext);
        SendEndpointProvider = new InMemoryOutboxSendEndpointProvider(outboxContext, context.SendEndpointProvider);

        PublishEndpointProvider = new InMemoryOutboxPublishEndpointProvider(outboxContext, context.PublishEndpointProvider);
    }

    /// <summary>Gets the provider that defers publishes through the outbox.</summary>
    public override IPublishEndpointProvider PublishEndpointProvider { get; }

    /// <summary>Gets the provider that defers addressed sends through the outbox.</summary>
    public override ISendEndpointProvider SendEndpointProvider { get; }
}
