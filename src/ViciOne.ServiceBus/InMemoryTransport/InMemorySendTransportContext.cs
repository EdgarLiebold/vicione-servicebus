using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

public class InMemorySendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<PipeContext>
{
    static readonly DateTimeOffsetTypeConverter _dateTimeOffsetConverter = new DateTimeOffsetTypeConverter();
    static readonly DateTimeTypeConverter _dateTimeConverter = new DateTimeTypeConverter();
    readonly IInMemoryDelayProvider _delayProvider;
    readonly IMessageExchange<InMemoryTransportMessage> _exchange;

    public InMemorySendTransportContext(IHostConfiguration hostConfiguration, ReceiveEndpointContext context,
        IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
        : base(hostConfiguration, context.Serialization)
    {
        _exchange = exchange;
        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));
    }

    public override string EntityName => _exchange.Name;
    public override string ActivitySystem => "in-memory";

    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        var sendContext = new InMemorySendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        return sendContext;
    }

    public Task<SendContext<T>> CreateSendContextAsync<T>(PipeContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    public Task SendAsync<T>(PipeContext transportContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        InMemorySendContext<T> context = sendContext as InMemorySendContext<T>
            ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        var messageId = context.MessageId ?? NewId.NextGuid();

        var body = context.Body ?? throw new InvalidOperationException("The send context body has not been serialized.");
        var contentType = context.ContentType ?? throw new InvalidOperationException("The send context content type has not been set.");
        var transportMessage = new InMemoryTransportMessage(messageId, body.GetBytes(), contentType.ToString())
        {
            Delay = context.Delay,
            RoutingKey = context.RoutingKey
        };

        if (context.TryGetPayload(out InMemoryDurableSendContext? durableSendContext))
            transportMessage.DurableSendContext = durableSendContext;

        SetHeaders(transportMessage.Headers, context.Headers);

        var deliveryContext = new InMemoryDeliveryContext(transportMessage, _delayProvider.UtcNow, context.CancellationToken);

        return _exchange.DeliverAsync(deliveryContext, cancellationToken: cancellationToken);
    }

    public Task SendAsync(IPipe<PipeContext> pipe, CancellationToken cancellationToken = default)
    {
        var pipeContext = new Context(cancellationToken);

        return pipe.SendAsync(pipeContext);
    }

    public void Probe(ProbeContext context)
    {
        _exchange.Probe(context);
    }

    static void SetHeaders(SendHeaders sendHeaders, Headers headers)
    {
        foreach (KeyValuePair<string, object> header in headers.GetAll())
        {
            if (header.Value == null)
            {
                sendHeaders.Set(header.Key, null);

                continue;
            }

            if (sendHeaders.TryGetHeader(header.Key, out _))
                continue;

            switch (header.Value)
            {
                case DateTimeOffset value:
                    if (_dateTimeOffsetConverter.TryConvert(value, out string text))
                        sendHeaders.Set(header.Key, text);
                    break;

                case DateTime value:
                    if (_dateTimeConverter.TryConvert(value, out text))
                        sendHeaders.Set(header.Key, text);
                    break;

                case Uri value:
                    sendHeaders.Set(header.Key, value.ToString());
                    break;

                case string value:
                    sendHeaders.Set(header.Key, value);
                    break;

                case bool value when value:
                    sendHeaders.Set(header.Key, bool.TrueString);
                    break;

                case IFormattable formatValue:
                    if (header.Value.GetType().IsValueType)
                        sendHeaders.Set(header.Key, header.Value);
                    else
                        sendHeaders.Set(header.Key, formatValue.ToString());
                    break;
            }
        }
    }


    class Context :
        BasePipeContext
    {
        public Context(CancellationToken cancellationToken)
            : base(cancellationToken)
        {
        }
    }
}
