using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>
/// Provides an in memory outbox receive context implementation.
/// </summary>
public class InMemoryOutboxReceiveContext :
    ReceiveContextProxy
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="outboxContext">The outbox context value.</param>
    /// <param name="context">The operation context.</param>
    public InMemoryOutboxReceiveContext(OutboxContext outboxContext, ReceiveContext context)
        : base(context)
    {
        SendEndpointProvider = new InMemoryOutboxSendEndpointProvider(outboxContext, context.SendEndpointProvider);

        PublishEndpointProvider = new InMemoryOutboxPublishEndpointProvider(outboxContext, context.PublishEndpointProvider);
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
