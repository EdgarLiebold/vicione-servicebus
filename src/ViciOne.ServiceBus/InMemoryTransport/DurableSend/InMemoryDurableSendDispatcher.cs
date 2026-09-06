using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Dispatches a retained serialized message into the volatile InMemory transport and reports consumer completion as
/// the only valid local completion boundary.
/// </summary>
/// <typeparam name="TBus">The bus type.</typeparam>
internal sealed class InMemoryDurableSendDispatcher<TBus> : IDurableSendDispatcher<TBus>
    where TBus : class, IBus
{
    readonly TBus _bus;
    readonly IEnumerable<IMessageContractCatalog> _contractCatalogs;

    public InMemoryDurableSendDispatcher(TBus bus, IEnumerable<IMessageContractCatalog> contractCatalogs)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _contractCatalogs = contractCatalogs ?? throw new ArgumentNullException(nameof(contractCatalogs));
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
        var pipe = new InMemoryDurableSendPipe(message, messageType, context);
        await endpoint.SendAsync(new SerializedMessageBody(), pipe, cancellationToken).ConfigureAwait(false);
        return DurableSendDispatchResult.AwaitConsumerCompletion;
    }

    sealed class InMemoryDurableSendPipe : IPipe<SendContext<SerializedMessageBody>>
    {
        readonly DurableSendDispatchContext _dispatchContext;
        readonly SerializedDurableSend _message;
        readonly Type _messageType;

        public InMemoryDurableSendPipe(
            SerializedDurableSend message,
            Type messageType,
            DurableSendDispatchContext dispatchContext)
        {
            _message = message;
            _messageType = messageType;
            _dispatchContext = dispatchContext;
        }

        public Task SendAsync(SendContext<SerializedMessageBody> context)
        {
            var contentType = new ContentType(_message.ContentType);
            context.Serializer = new CopyBodySerializer(contentType, new MemoryMessageBody(_message.Body));
            context.ContentType = contentType;
            ReliableEnvelopeMetadataCodec.Apply(context, _message.Metadata, context.GetTimeProvider().GetUtcNow());
            context.MessageId = _message.MessageId;
            context.CorrelationId = _message.CorrelationId;
            context.Durable = true;
            context.SupportedMessageTypes = [MessageUrn.ForTypeString(_messageType)];
            context.GetOrAddPayload(() => new InMemoryDurableSendContext(
                _dispatchContext.DurableSendId,
                _message.ContractIdentity,
                _dispatchContext.Attempt,
                _message.Metadata,
                _dispatchContext.ConsumerCompletion));
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
            => context.CreateFilterScope("inMemoryDurableSend");
    }
}
