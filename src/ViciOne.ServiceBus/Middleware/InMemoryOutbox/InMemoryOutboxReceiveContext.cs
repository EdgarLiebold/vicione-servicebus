using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Carries state for in memory outbox receive operations.</summary>
public class InMemoryOutboxReceiveContext :
    ReceiveContextProxy
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="outboxContext">The outbox context.</param>
    /// <param name="context">The context associated with the operation.</param>
    public InMemoryOutboxReceiveContext(OutboxContext outboxContext, ReceiveContext context)
        : base(context)
    {
        SendEndpointProvider = new InMemoryOutboxSendEndpointProvider(outboxContext, context.SendEndpointProvider);

        PublishEndpointProvider = new InMemoryOutboxPublishEndpointProvider(outboxContext, context.PublishEndpointProvider);
    }

    /// <summary>Gets the publish endpoint provider.</summary>
    public override IPublishEndpointProvider PublishEndpointProvider { get; }

    /// <summary>Gets the send endpoint provider.</summary>
    public override ISendEndpointProvider SendEndpointProvider { get; }
}
