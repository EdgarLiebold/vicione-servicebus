#nullable enable

namespace ViciOne.ServiceBus.RabbitMqTransport;

using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.DurableSend;
using ViciOne.ServiceBus.Serialization;

/// <summary>
/// Replays the exact retained envelope through RabbitMQ and reports only its durable broker-acceptance boundary.
/// </summary>
internal sealed class RabbitMqDurableSendDispatcher<TBus> : IDurableSendDispatcher<TBus>
    where TBus : class, IBus
{
    private readonly TBus _bus;
    private readonly IEnumerable<IMessageContractCatalog> _contractCatalogs;

    public RabbitMqDurableSendDispatcher(TBus bus, IEnumerable<IMessageContractCatalog> contractCatalogs)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _contractCatalogs = contractCatalogs ?? throw new ArgumentNullException(nameof(contractCatalogs));

        if (!IsRabbitMqScheme(bus.Address.Scheme))
        {
            throw new ConfigurationException(
                $"Durable Sender for bus '{typeof(TBus)}' selected the RabbitMQ transport adapter, but the owning " +
                $"bus address '{bus.Address}' is not a RabbitMQ address. Configure UsingRabbitMq(...) and " +
                "UseDurableSender(...) on the same bus.");
        }
    }

    public async Task<DurableSendDispatchResult> DispatchAsync(
        DurableSendDispatchContext context,
        CancellationToken cancellationToken = default)
    {
        SerializedDurableSend message = context.Message.Validate();
        IMessageContractCatalog contractCatalog =
            DurableSenderComposition.RequireExactlyOne<IMessageContractCatalog, TBus>(
                _contractCatalogs,
                "message-contract catalog");
        if (!contractCatalog.TryGetMessageType(message.ContractIdentity, out Type? messageType))
        {
            throw new MessageContractException(
                $"Durable send contract identity '{message.ContractIdentity}' is not registered in the immutable message contract catalog.");
        }

        ISendEndpoint endpoint = await _bus.GetSendEndpoint(message.DestinationAddress).ConfigureAwait(false);
        await endpoint.Send(
                new SerializedMessageBody(),
                new RabbitMqDurableSendPipe(message, messageType!),
                cancellationToken)
            .ConfigureAwait(false);

        // RabbitMqDurableSendPipe forces persistent + mandatory + publisher-confirm acknowledgement. The transport's
        // Send task completes only after RabbitMQ.Client has observed that boundary; returns/nacks surface as errors.
        return DurableSendDispatchResult.TransportAccepted;
    }

    private static bool IsRabbitMqScheme(string scheme) =>
        scheme.Equals(RabbitMqHostAddress.RabbitMqScheme, StringComparison.OrdinalIgnoreCase)
        || scheme.Equals(RabbitMqHostAddress.RabbitMqSecureScheme, StringComparison.OrdinalIgnoreCase)
        || scheme.Equals("amqp", StringComparison.OrdinalIgnoreCase)
        || scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase);

    private sealed class RabbitMqDurableSendPipe : IPipe<SendContext<SerializedMessageBody>>
    {
        private readonly SerializedDurableSend _message;
        private readonly Type _messageType;

        public RabbitMqDurableSendPipe(SerializedDurableSend message, Type messageType)
        {
            _message = message;
            _messageType = messageType;
        }

        public Task Send(SendContext<SerializedMessageBody> context)
        {
            var contentType = new ContentType(_message.ContentType);
            context.Serializer = new CopyBodySerializer(contentType, new MemoryMessageBody(_message.Body));
            context.ContentType = contentType;
            context.MessageId = _message.MessageId;
            context.CorrelationId = _message.CorrelationId;
            context.Durable = true;
            if (!context.TryGetPayload(out RabbitMqSendContext? rabbitMqContext))
            {
                throw new ConfigurationException(
                    "The RabbitMQ Durable Sender adapter did not receive a RabbitMqSendContext from its owning transport.");
            }
            rabbitMqContext.Mandatory = true;
            rabbitMqContext.AwaitAck = true;
            context.SupportedMessageTypes = [MessageUrn.ForTypeString(_messageType)];
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("rabbitMqDurableSend");
    }
}
