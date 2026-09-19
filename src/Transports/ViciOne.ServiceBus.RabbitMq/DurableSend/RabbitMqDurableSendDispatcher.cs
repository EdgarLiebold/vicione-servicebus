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

        RabbitMqEndpointAddress destination = ValidateDestination(message.DestinationAddress);
        var acceptanceRequirement = new RabbitMqTransportAcceptanceRequirement(destination.Name, requiresExistingQueueProof: true);

        ISendEndpoint endpoint = await _bus.GetSendEndpointAsync(message.DestinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);
        await endpoint.SendAsync(
                SerializedTransportMessage.Instance,
                new RabbitMqDurableSendPipe(message, messageType!, acceptanceRequirement),
                cancellationToken)
            .ConfigureAwait(false);

        if (!acceptanceRequirement.Accepted)
        {
            throw new InvalidOperationException(
                "The RabbitMQ durable send completed without a publisher-confirmed transport acceptance.");
        }

        // The transport validates that publisher confirmations are enabled before publishing and
        // completes the send only after RabbitMQ has confirmed the persistent, mandatory publish.
        return DurableSendDispatchResult.TransportAccepted;
    }

    private RabbitMqEndpointAddress ValidateDestination(Uri destinationAddress)
    {
        var address = new RabbitMqEndpointAddress(_bus.Address, destinationAddress);
        bool canProveExistingReceiveQueue = IsRabbitMqScheme(destinationAddress.Scheme)
            && !address.BindToQueue
            && address.QueueName is null
            && address.BindExchanges.Count == 0;
        if (canProveExistingReceiveQueue && address.Durable && !address.AutoDelete
            && address.ExchangeType.Equals(RabbitMQ.Client.ExchangeType.Fanout, StringComparison.OrdinalIgnoreCase)
            && address.AlternateExchange is null
            && !IsDirectReplyTo(address.Name)
            && (address.QueueName is null || !IsDirectReplyTo(address.QueueName)))
            return address;

        throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                "RabbitMQ durable transport acceptance",
                destinationAddress.ToString(),
                "The destination does not identify an existing same-name durable quorum receive queue, or it uses queue declaration, an alternate exchange, or RabbitMQ direct reply-to",
                "Use the existing receive queue's RabbitMQ exchange-form address without an alternate exchange instead"));
    }

    private static bool IsDirectReplyTo(string name) =>
        name.Equals(RabbitMqExchangeNames.ReplyTo, StringComparison.OrdinalIgnoreCase)
        || name.StartsWith(RabbitMqExchangeNames.ReplyTo + ".", StringComparison.OrdinalIgnoreCase);

    private static bool IsRabbitMqScheme(string scheme) =>
        scheme.Equals(RabbitMqHostAddress.RabbitMqScheme, StringComparison.OrdinalIgnoreCase)
        || scheme.Equals(RabbitMqHostAddress.RabbitMqSecureScheme, StringComparison.OrdinalIgnoreCase)
        || scheme.Equals("amqp", StringComparison.OrdinalIgnoreCase)
        || scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase);

    private sealed class RabbitMqDurableSendPipe : IPipe<SendContext<SerializedTransportMessage>>
    {
        private readonly SerializedDurableSend _message;
        private readonly Type _messageType;
        private readonly RabbitMqTransportAcceptanceRequirement _acceptanceRequirement;

        public RabbitMqDurableSendPipe(SerializedDurableSend message, Type messageType,
            RabbitMqTransportAcceptanceRequirement acceptanceRequirement)
        {
            _message = message;
            _messageType = messageType;
            _acceptanceRequirement = acceptanceRequirement;
        }

        public Task SendAsync(SendContext<SerializedTransportMessage> context)
        {
            var contentType = new ContentType(_message.ContentType);
            context.ContentType = contentType;
            DurablePayloadAdmissionProof? proof = ReliableEnvelopeMetadataCodec.ApplyForDurableReplay(
                context,
                _message.Metadata,
                context.GetTimeProvider().GetUtcNow());
            context.Serializer = new CopyBodySerializer(_message.ContentType, _message.Body, proof);
            context.MessageId = _message.MessageId;
            context.CorrelationId = _message.CorrelationId;
            context.Durable = true;
            context.GetOrAddPayload(() => _acceptanceRequirement);
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
