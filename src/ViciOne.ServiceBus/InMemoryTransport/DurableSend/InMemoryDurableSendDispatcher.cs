using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Dispatches a retained serialized message into the volatile InMemory transport and reports consumer completion as
/// the only valid local-retirement boundary.
/// </summary>
internal sealed class InMemoryDurableSendDispatcher<TBus> : IDurableSendDispatcher<TBus>
    where TBus : class, IBus
{
    readonly TBus _bus;
    readonly IMessageContractCatalog _contractCatalog;

    public InMemoryDurableSendDispatcher(TBus bus, IMessageContractCatalog contractCatalog)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _contractCatalog = contractCatalog ?? throw new ArgumentNullException(nameof(contractCatalog));
    }

    public async Task<DurableSendDispatchResult> DispatchAsync(
        DurableSendDispatchContext context,
        CancellationToken cancellationToken = default)
    {
        SerializedDurableSend message = context.Message.Validate();
        if (!_contractCatalog.TryGetMessageType(message.ContractIdentity, out Type messageType))
        {
            throw new MessageContractException(
                $"Durable send contract identity '{message.ContractIdentity}' is not registered in the immutable message contract catalog.");
        }

        ISendEndpoint endpoint = await _bus.GetSendEndpoint(message.DestinationAddress).ConfigureAwait(false);
        var pipe = new InMemoryDurableSendPipe(message, messageType, context);
        await endpoint.Send(new SerializedMessageBody(), pipe, cancellationToken).ConfigureAwait(false);
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

        public Task Send(SendContext<SerializedMessageBody> context)
        {
            var contentType = new ContentType(_message.ContentType);
            context.Serializer = new CopyBodySerializer(contentType, new MemoryMessageBody(_message.Body));
            context.ContentType = contentType;
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
