using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Adapts a received Apache NMS message to the transport receive context.</summary>
public sealed class ActiveMqReceiveContext :
    BaseReceiveContext,
    ActiveMqMessageContext,
    TransportReceiveContext
{
    readonly MessageBody _body;

    /// <summary>Creates a receive context for a native ActiveMQ message.</summary>
    /// <param name="transportMessage">The received Apache NMS message.</param>
    /// <param name="context">The ActiveMQ receive-endpoint context.</param>
    /// <param name="payloads">Additional context payloads.</param>
    public ActiveMqReceiveContext(IMessage transportMessage, ActiveMqReceiveEndpointContext context, params object[] payloads)
        : base(transportMessage.NMSRedelivered, context, payloads)
    {
        TransportMessage = transportMessage;

        _body = new ActiveMqMessageBody(transportMessage);
    }

    /// <summary>Gets a provider for the native message headers.</summary>
    protected override IHeaderProvider HeaderProvider => new ActiveMqHeaderProvider(TransportMessage);

    /// <summary>Gets the message body with the configured receive limits enforced.</summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    /// <summary>Gets the underlying Apache NMS message.</summary>
    public IMessage TransportMessage { get; }

    /// <summary>Gets the native message-property map.</summary>
    public IPrimitiveMap Properties => TransportMessage.Properties;

    /// <summary>Gets the OpenTelemetry messaging-system identifier.</summary>
    public string ActivitySystem => "activemq";

    /// <summary>Gets the JMSX message-group identifier.</summary>
    public string? GroupId => TransportMessage.GetGroupId();

    /// <summary>Gets the JMSX message-group sequence number.</summary>
    public int GroupSequence => TransportMessage.GetGroupSequence();

    /// <summary>Gets the non-default ActiveMQ delivery properties recorded for diagnostics.</summary>
    /// <returns>The priority and message-group properties, or <see langword="null" /> when all use their defaults.</returns>
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

    /// <summary>Gets a send-endpoint provider that preserves the message's native reply destination.</summary>
    /// <returns>The endpoint's provider, optionally decorated with the native reply destination.</returns>
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
