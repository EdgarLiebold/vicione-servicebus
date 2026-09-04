using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>
/// Provides an outbox receive context implementation.
/// </summary>
public class OutboxReceiveContext :
    ReceiveContextProxy
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="outboxContext">The outbox context value.</param>
    /// <param name="context">The operation context.</param>
    public OutboxReceiveContext(OutboxSendContext outboxContext, ReceiveContext context)
        : base(context)
    {
        SendEndpointProvider = new OutboxSendEndpointProvider(outboxContext, context.SendEndpointProvider);
        PublishEndpointProvider = new OutboxPublishEndpointProvider(outboxContext, context.PublishEndpointProvider);
    }

    /// <summary>
    /// Gets the publish endpoint provider value.
    /// </summary>
    public override IPublishEndpointProvider PublishEndpointProvider { get; }
    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    public override ISendEndpointProvider SendEndpointProvider { get; }
}
