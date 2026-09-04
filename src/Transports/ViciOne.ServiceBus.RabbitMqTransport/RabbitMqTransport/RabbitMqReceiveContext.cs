using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public sealed class RabbitMqReceiveContext :
    BaseReceiveContext,
    RabbitMqBasicConsumeContext,
    TransportReceiveContext,
    ITransportSequenceNumber
{
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

    protected override IHeaderProvider HeaderProvider => new RabbitMqHeaderProvider(this);

    public override MessageBody Body { get; }

    public ulong? SequenceNumber => DeliveryTag;

    public string ConsumerTag { get; }
    public ulong DeliveryTag { get; }
    public string Exchange { get; }
    public string RoutingKey { get; }
    public IReadOnlyBasicProperties Properties { get; }

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

    protected override ContentType GetContentType()
    {
        ContentType? contentType = default;
        if (!string.IsNullOrWhiteSpace(Properties.ContentType))
            contentType = ConvertToContentType(Properties.ContentType);

        return contentType ?? base.GetContentType();
    }

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
