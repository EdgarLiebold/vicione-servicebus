using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Carries state for outbox receive operations.</summary>
public class OutboxReceiveContext :
    ReceiveContextProxy
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="outboxContext">The outbox context.</param>
    /// <param name="context">The context associated with the operation.</param>
    public OutboxReceiveContext(OutboxSendContext outboxContext, ReceiveContext context)
        : base(context)
    {
        SendEndpointProvider = new OutboxSendEndpointProvider(outboxContext, context.SendEndpointProvider);
        PublishEndpointProvider = new OutboxPublishEndpointProvider(outboxContext, context.PublishEndpointProvider);
    }

    /// <summary>Gets the publish endpoint provider.</summary>
    public override IPublishEndpointProvider PublishEndpointProvider { get; }
    /// <summary>Gets the send endpoint provider.</summary>
    public override ISendEndpointProvider SendEndpointProvider { get; }
}
