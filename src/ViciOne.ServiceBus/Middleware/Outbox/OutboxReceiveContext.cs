// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware.Outbox
{
    using Context;


    public class OutboxReceiveContext :
        ReceiveContextProxy
    {
        public OutboxReceiveContext(OutboxSendContext outboxContext, ReceiveContext context)
            : base(context)
        {
            SendEndpointProvider = new OutboxSendEndpointProvider(outboxContext, context.SendEndpointProvider);
            PublishEndpointProvider = new OutboxPublishEndpointProvider(outboxContext, context.PublishEndpointProvider);
        }

        public override IPublishEndpointProvider PublishEndpointProvider { get; }
        public override ISendEndpointProvider SendEndpointProvider { get; }
    }
}
