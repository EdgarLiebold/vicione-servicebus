using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Redirects send and publish endpoint resolution through a durable outbox.</summary>
internal sealed class OutboxReceiveContext :
    ReceiveContextProxy
{
    /// <summary>Initializes the decorator over an existing receive context.</summary>
    /// <param name="outboxContext">The outbox context that captures outgoing messages.</param>
    /// <param name="context">The receive context whose endpoint providers are decorated.</param>
    public OutboxReceiveContext(OutboxSendContext outboxContext, ReceiveContext context)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        ArgumentNullException.ThrowIfNull(outboxContext);
        SendEndpointProvider = new OutboxSendEndpointProvider(outboxContext, context.SendEndpointProvider);
        PublishEndpointProvider = new OutboxPublishEndpointProvider(outboxContext, context.PublishEndpointProvider);
    }

    /// <summary>Gets the provider that captures publishes in the outbox.</summary>
    public override IPublishEndpointProvider PublishEndpointProvider { get; }
    /// <summary>Gets the provider that captures addressed sends in the outbox.</summary>
    public override ISendEndpointProvider SendEndpointProvider { get; }
}
