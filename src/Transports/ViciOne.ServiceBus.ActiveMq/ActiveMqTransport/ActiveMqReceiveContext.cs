using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides an active mq receive context implementation.
/// </summary>
public sealed class ActiveMqReceiveContext :
    BaseReceiveContext,
    ActiveMqMessageContext,
    TransportReceiveContext
{
    readonly MessageBody _body;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="transportMessage">The transport message value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="payloads">The payloads value.</param>
    public ActiveMqReceiveContext(IMessage transportMessage, ActiveMqReceiveEndpointContext context, params object[] payloads)
        : base(transportMessage.NMSRedelivered, context, payloads)
    {
        TransportMessage = transportMessage;

        _body = new ActiveMqMessageBody(transportMessage);
    }

    /// <summary>
    /// Gets the header provider value.
    /// </summary>
    protected override IHeaderProvider HeaderProvider => new ActiveMqHeaderProvider(TransportMessage);

    /// <summary>
    /// Gets the body value.
    /// </summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    /// <summary>
    /// Gets the transport message value.
    /// </summary>
    public IMessage TransportMessage { get; }

    /// <summary>
    /// Gets the properties value.
    /// </summary>
    public IPrimitiveMap Properties => TransportMessage.Properties;

    /// <summary>
    /// Gets the activity system value.
    /// </summary>
    public string ActivitySystem => "activemq";

    /// <summary>
    /// Gets the group id value.
    /// </summary>
    public string? GroupId => TransportMessage.GetGroupId();

    /// <summary>
    /// Gets the group sequence value.
    /// </summary>
    public int GroupSequence => TransportMessage.GetGroupSequence();

    /// <summary>
    /// Gets transport properties.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Gets send endpoint provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
