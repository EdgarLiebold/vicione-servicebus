// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport.Middleware
{
    /// <summary>
    /// Creates a message session receiver
    /// </summary>
    public class MessageSessionReceiverFilter :
        MessageReceiverFilter
    {
        public MessageSessionReceiverFilter(ServiceBusReceiveEndpointContext context)
            : base(context)
        {
        }

        public override void Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("messageSessionReceiver");
            scope.Add("type", "brokeredMessage");

            Context.ReceivePipe.Probe(scope);
        }

        protected override IReceiver CreateMessageReceiver(ClientContext context)
        {
            return new SessionReceiver(context, Context);
        }
    }
}
