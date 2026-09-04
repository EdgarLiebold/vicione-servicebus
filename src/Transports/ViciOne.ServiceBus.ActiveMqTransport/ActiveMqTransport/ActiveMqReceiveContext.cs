using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public sealed class ActiveMqReceiveContext :
    BaseReceiveContext,
    ActiveMqMessageContext,
    TransportReceiveContext
{
    public ActiveMqReceiveContext(IMessage transportMessage, ActiveMqReceiveEndpointContext context, params object[] payloads)
        : base(transportMessage.NMSRedelivered, context, payloads)
    {
        TransportMessage = transportMessage;

        Body = new ActiveMqMessageBody(transportMessage);
    }

    protected override IHeaderProvider HeaderProvider => new ActiveMqHeaderProvider(TransportMessage);

    public override MessageBody Body { get; }

    public IMessage TransportMessage { get; }

    public IPrimitiveMap Properties => TransportMessage.Properties;

    public string ActivitySystem => "activemq";

    public string? GroupId => TransportMessage.GetGroupId();

    public int GroupSequence => TransportMessage.GetGroupSequence();

    public IDictionary<string, object>? GetTransportProperties()
    {
        var properties = new Lazy<Dictionary<string, object>>(() => new Dictionary<string, object>());

        if (TransportMessage.NMSPriority != MsgPriority.Normal)
            properties.Value[ActiveMqTransportPropertyNames.Priority] = TransportMessage.NMSPriority.ToString();
        if (GroupId != null)
            properties.Value[ActiveMqTransportPropertyNames.GroupId] = GroupId;
        if (GroupSequence != default)
            properties.Value[ActiveMqTransportPropertyNames.GroupSequence] = GroupSequence;

        return properties.IsValueCreated ? properties.Value : null;
    }

    protected override ISendEndpointProvider GetSendEndpointProvider()
    {
        var provider = base.GetSendEndpointProvider();

        return TransportMessage.NMSReplyTo != null
            ? new ReceiveSendEndpointProvider(provider, TransportMessage.NMSReplyTo)
            : provider;
    }


    class ReceiveSendEndpointProvider :
        ISendEndpointProvider
    {
        readonly IDestination _replyTo;
        readonly ISendEndpointProvider _sendEndpointProvider;

        public ReceiveSendEndpointProvider(ISendEndpointProvider sendEndpointProvider, IDestination replyTo)
        {
            _replyTo = replyTo;

            _sendEndpointProvider = sendEndpointProvider;
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            return _sendEndpointProvider.ConnectSendObserver(observer);
        }

        public async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);

            return new ReplyToSendEndpoint(endpoint, _replyTo);
        }
    }
}
