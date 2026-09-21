using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Adapts one RabbitMQ delivery to the transport receive context.</summary>
public sealed class RabbitMqReceiveContext :
    BaseReceiveContext,
    RabbitMqBasicConsumeContext,
    TransportReceiveContext,
    ITransportSequenceNumber
{
    readonly MessageBody _body;

    /// <summary>Creates a receive context from immutable AMQP delivery data.</summary>
    /// <param name="exchange">The source exchange.</param>
    /// <param name="routingKey">The delivery routing key.</param>
    /// <param name="consumerTag">The broker-assigned consumer tag.</param>
    /// <param name="deliveryTag">The channel-scoped delivery tag.</param>
    /// <param name="body">The serialized message body.</param>
    /// <param name="redelivered">Whether RabbitMQ previously delivered this message.</param>
    /// <param name="properties">The immutable AMQP message properties.</param>
    /// <param name="receiveEndpointContext">The endpoint context and message limits.</param>
    /// <param name="payloads">Additional transport payloads.</param>
    public RabbitMqReceiveContext(string exchange, string routingKey, string consumerTag, ulong deliveryTag, ReadOnlyMemory<byte> body,
        bool redelivered, IReadOnlyBasicProperties properties, RabbitMqReceiveEndpointContext receiveEndpointContext, params object[] payloads)
        : base(redelivered, receiveEndpointContext, payloads)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
        ConsumerTag = consumerTag;
        DeliveryTag = deliveryTag;
        Properties = properties;

        _body = new BinaryMessageBody(body);
    }

    /// <summary>Gets a header provider over the immutable AMQP properties.</summary>
    protected override IHeaderProvider HeaderProvider => new RabbitMqHeaderProvider(this);

    /// <summary>Gets the body after enforcing configured transport message limits.</summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    /// <summary>Gets the channel-scoped delivery tag as the transport sequence number.</summary>
    public ulong? SequenceNumber => DeliveryTag;

    /// <summary>Gets the tag of the RabbitMQ consumer that received the delivery.</summary>
    public string ConsumerTag { get; }
    /// <summary>Gets the channel-scoped delivery tag used for acknowledgement.</summary>
    public ulong DeliveryTag { get; }
    /// <summary>Gets the exchange from which RabbitMQ routed the delivery.</summary>
    public string Exchange { get; }
    /// <summary>Gets the routing key attached to the delivery.</summary>
    public string RoutingKey { get; }
    /// <summary>Gets the immutable AMQP message properties.</summary>
    public IReadOnlyBasicProperties Properties { get; }

    /// <summary>Captures nonempty RabbitMQ routing and AMQP properties for later replay.</summary>
    /// <returns>The transport-property bag, or <see langword="null" /> when no values are present.</returns>
    public IDictionary<string, object>? GetTransportProperties()
    {
        var properties = new Lazy<Dictionary<string, object>>(() => new Dictionary<string, object>());

        if (!string.IsNullOrWhiteSpace(RoutingKey))
            properties.Value[RabbitMqTransportPropertyNames.RoutingKey] = RoutingKey;

        if (Properties.IsAppIdPresent() && !string.IsNullOrWhiteSpace(Properties.AppId))
        {
            var appId = Properties.AppId;
            properties.Value[RabbitMqTransportPropertyNames.AppId] = appId;
        }
        if (Properties.IsPriorityPresent())
            properties.Value[RabbitMqTransportPropertyNames.Priority] = Properties.Priority;
        if (Properties.IsReplyToPresent() && !string.IsNullOrWhiteSpace(Properties.ReplyTo))
        {
            var replyTo = Properties.ReplyTo;
            properties.Value[RabbitMqTransportPropertyNames.ReplyTo] = replyTo;
        }
        if (Properties.IsTypePresent() && !string.IsNullOrWhiteSpace(Properties.Type))
        {
            var type = Properties.Type;
            properties.Value[RabbitMqTransportPropertyNames.Type] = type;
        }
        if (Properties.IsUserIdPresent() && !string.IsNullOrWhiteSpace(Properties.UserId))
        {
            var userId = Properties.UserId;
            properties.Value[RabbitMqTransportPropertyNames.UserId] = userId;
        }

        return properties.IsValueCreated ? properties.Value : null;
    }

    /// <summary>Gets the AMQP content type or the base transport default.</summary>
    /// <returns>The parsed message content type.</returns>
    protected override ContentType GetContentType()
    {
        ContentType? contentType = default;
        if (!string.IsNullOrWhiteSpace(Properties.ContentType))
            contentType = ConvertToContentType(Properties.ContentType);

        return contentType ?? base.GetContentType();
    }

    /// <summary>Wraps the send endpoint provider when the delivery carries a direct-reply-to address.</summary>
    /// <returns>The endpoint provider for sends from this consume context.</returns>
    protected override ISendEndpointProvider GetSendEndpointProvider()
    {
        var provider = base.GetSendEndpointProvider();

        return Properties.IsReplyToPresent() && !string.IsNullOrWhiteSpace(Properties.ReplyTo)
            ? new ReceiveSendEndpointProvider(provider, Properties.ReplyTo)
            : provider;
    }


    class ReceiveSendEndpointProvider :
        ISendEndpointProvider
    {
        readonly string _replyTo;
        readonly ISendEndpointProvider _sendEndpointProvider;

        public ReceiveSendEndpointProvider(ISendEndpointProvider sendEndpointProvider, string replyTo)
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

            return address.IsReplyToAddress()
                ? new ReplyToSendEndpoint(endpoint, _replyTo)
                : endpoint;
        }
    }
}
