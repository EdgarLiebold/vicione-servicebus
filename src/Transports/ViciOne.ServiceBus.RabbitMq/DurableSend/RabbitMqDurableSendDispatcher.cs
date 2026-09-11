using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Replays the exact retained envelope through RabbitMQ and reports acceptance only after broker confirmation.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
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
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"Durable Sender for bus '{typeof(TBus)}' selected the RabbitMQ transport adapter, but the owning " +
                $"bus address '{bus.Address}' is not a RabbitMQ address. Configure UsingRabbitMq(...) and " +
                "UseReliableMessaging(...) on the same bus.", "Correct the named configuration before starting the host"));
        }
    }

    public async Task<DurableSendDispatchResult> DispatchAsync(
        DurableSendDispatchContext context,
        CancellationToken cancellationToken = default)
    {
        SerializedDurableSend message = context.Message.Validate();
        IMessageContractCatalog contractCatalog =
            ReliableMessagingComposition.RequireExactlyOne<IMessageContractCatalog, TBus>(
                _contractCatalogs,
                "message-contract catalog");
        if (!contractCatalog.TryGetMessageType(message.ContractIdentity, out Type? messageType))
        {
            throw new MessageContractException(
                $"Durable send contract identity '{message.ContractIdentity}' is not registered in the immutable message contract catalog.");
        }

        ISendEndpoint endpoint = await _bus.GetSendEndpointAsync(message.DestinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);
        await endpoint.SendAsync(
                SerializedTransportMessage.Instance,
                new RabbitMqDurableSendPipe(message, messageType!),
                cancellationToken)
            .ConfigureAwait(false);

        // The transport validates that publisher confirmations are enabled before publishing and
        // completes the send only after RabbitMQ has confirmed the persistent, mandatory publish.
        return DurableSendDispatchResult.TransportAccepted;
    }

    private static bool IsRabbitMqScheme(string scheme) =>
        scheme.Equals(RabbitMqHostAddress.RabbitMqScheme, StringComparison.OrdinalIgnoreCase)
        || scheme.Equals(RabbitMqHostAddress.RabbitMqSecureScheme, StringComparison.OrdinalIgnoreCase)
        || scheme.Equals("amqp", StringComparison.OrdinalIgnoreCase)
        || scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase);

    private sealed class RabbitMqDurableSendPipe : IPipe<SendContext<SerializedTransportMessage>>
    {
        private readonly SerializedDurableSend _message;
        private readonly Type _messageType;

        public RabbitMqDurableSendPipe(SerializedDurableSend message, Type messageType)
        {
            _message = message;
            _messageType = messageType;
        }

        public Task SendAsync(SendContext<SerializedTransportMessage> context)
        {
            var contentType = new ContentType(_message.ContentType);
            context.Serializer = new CopyBodySerializer(contentType, new MemoryMessageBody(_message.Body));
            context.ContentType = contentType;
            ReliableEnvelopeMetadataCodec.Apply(context, _message.Metadata, context.GetTimeProvider().GetUtcNow());
            context.MessageId = _message.MessageId;
            context.CorrelationId = _message.CorrelationId;
            context.Durable = true;
            context.GetOrAddPayload(() => RabbitMqTransportAcceptanceRequirement.Instance);
            if (!context.TryGetPayload(out RabbitMqSendContext? rabbitMqContext))
            {
                throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "The RabbitMQ Durable Sender adapter did not receive a RabbitMqSendContext from its owning transport.", "Correct the named configuration before starting the host"));
            }
            rabbitMqContext.Mandatory = true;
            rabbitMqContext.AwaitAck = true;
            context.SupportedMessageTypes = [MessageUrn.ForTypeString(_messageType)];
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("rabbitMqDurableSend");
    }
}
