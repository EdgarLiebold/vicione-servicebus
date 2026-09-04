using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq receive context implementation.
/// </summary>
public sealed class RabbitMqReceiveContext :
    BaseReceiveContext,
    RabbitMqBasicConsumeContext,
    TransportReceiveContext,
    ITransportSequenceNumber
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="deliveryTag">The delivery tag value.</param>
    /// <param name="body">The body value.</param>
    /// <param name="redelivered">The redelivered value.</param>
    /// <param name="properties">The properties value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="payloads">The payloads value.</param>
    public RabbitMqReceiveContext(string exchange, string routingKey, string consumerTag, ulong deliveryTag, ReadOnlyMemory<byte> body,
        bool redelivered, IReadOnlyBasicProperties properties, RabbitMqReceiveEndpointContext receiveEndpointContext, params object[] payloads)
        : base(redelivered, receiveEndpointContext, payloads)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
        ConsumerTag = consumerTag;
        DeliveryTag = deliveryTag;
        Properties = properties;

        Body = new MemoryMessageBody(body);
    }

    /// <summary>
    /// Gets the header provider value.
    /// </summary>
    protected override IHeaderProvider HeaderProvider => new RabbitMqHeaderProvider(this);

    /// <summary>
    /// Gets the body value.
    /// </summary>
    public override MessageBody Body { get; }

    /// <summary>
    /// Gets the sequence number value.
    /// </summary>
    public ulong? SequenceNumber => DeliveryTag;

    /// <summary>
    /// Gets the consumer tag value.
    /// </summary>
    public string ConsumerTag { get; }
    /// <summary>
    /// Gets the delivery tag value.
    /// </summary>
    public ulong DeliveryTag { get; }
    /// <summary>
    /// Gets the exchange value.
    /// </summary>
    public string Exchange { get; }
    /// <summary>
    /// Gets the routing key value.
    /// </summary>
    public string RoutingKey { get; }
    /// <summary>
    /// Gets the properties value.
    /// </summary>
    public IReadOnlyBasicProperties Properties { get; }

    /// <summary>
    /// Gets transport properties.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IDictionary<string, object>? GetTransportProperties()
    {
        var properties = new Lazy<Dictionary<string, object>>(() => new Dictionary<string, object>());

        if (!string.IsNullOrWhiteSpace(RoutingKey))
            properties.Value[RabbitMqTransportPropertyNames.RoutingKey] = RoutingKey;

        if (Properties.IsAppIdPresent() && Properties.AppId is { } appId)
            properties.Value[RabbitMqTransportPropertyNames.AppId] = appId;
        if (Properties.IsPriorityPresent())
            properties.Value[RabbitMqTransportPropertyNames.Priority] = Properties.Priority;
        if (Properties.IsReplyToPresent() && Properties.ReplyTo is { } replyTo)
            properties.Value[RabbitMqTransportPropertyNames.ReplyTo] = replyTo;
        if (Properties.IsTypePresent() && Properties.Type is { } type)
            properties.Value[RabbitMqTransportPropertyNames.Type] = type;
        if (Properties.IsUserIdPresent() && Properties.UserId is { } userId)
            properties.Value[RabbitMqTransportPropertyNames.UserId] = userId;

        return properties.IsValueCreated ? properties.Value : null;
    }

    /// <summary>
    /// Gets content type.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override ContentType GetContentType()
    {
        ContentType? contentType = default;
        if (!string.IsNullOrWhiteSpace(Properties.ContentType))
            contentType = ConvertToContentType(Properties.ContentType);

        return contentType ?? base.GetContentType();
    }

    /// <summary>
    /// Gets send endpoint provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
