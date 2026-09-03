#nullable enable
namespace ViciOne.ServiceBus.InMemoryTransport
{
    using Transports;


    public sealed class InMemoryReceiveContext :
        BaseReceiveContext,
        RoutingKeyConsumeContext
    {
        readonly InMemoryTransportMessage _message;

        public InMemoryReceiveContext(InMemoryTransportMessage message, InMemoryReceiveEndpointContext receiveEndpointContext)
            : base(message.DeliveryCount > 0, receiveEndpointContext, GetPayloads(message))
        {
            _message = message;

            Body = new BytesMessageBody(message.Body);
        }

        protected override IHeaderProvider HeaderProvider => new DictionarySendHeaderProvider(_message.Headers);

        public override MessageBody Body { get; }
        public string? RoutingKey => _message.RoutingKey;

        static object[] GetPayloads(InMemoryTransportMessage message)
            => message.DurableSendContext is { } durableSendContext ? [durableSendContext] : [];
    }
}
