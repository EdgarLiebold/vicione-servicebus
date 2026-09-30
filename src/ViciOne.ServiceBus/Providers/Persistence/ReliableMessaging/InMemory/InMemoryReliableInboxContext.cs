using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed class InMemoryReliableInboxContext<TBus, TMessage> :
    OutboxConsumeContextProxy<TMessage>
    where TBus : class, IBus
    where TMessage : class
{
    readonly List<SerializedDurableSend> _messages = [];
    readonly Lock _messagesLock = new();
    readonly IMessageContractCatalog _contracts;
    readonly IServiceProvider _provider;
    readonly DurableSendStoreLimits _limits;
    readonly ReliableInboxKey _key;
    readonly ReliableInboxLease _lease;
    readonly int _receiveCount;
    readonly InMemoryReliableStore<TBus> _store;
    readonly TimeProvider _timeProvider;

    public InMemoryReliableInboxContext(
        ConsumeContext<TMessage> context,
        OutboxConsumeOptions options,
        IServiceProvider provider,
        InMemoryReliableStore<TBus> store,
        IMessageContractCatalog contracts,
        DurableSendStoreLimits limits,
        ReliableInboxKey key,
        ReliableInboxLease lease,
        int receiveCount,
        TimeProvider timeProvider)
        : base(
            context ?? throw new ArgumentNullException(nameof(context)),
            options ?? throw new ArgumentNullException(nameof(options)),
            provider ?? throw new ArgumentNullException(nameof(provider)))
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _provider = provider;
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
        _limits = limits;
        _key = key;
        _lease = lease;
        _receiveCount = receiveCount;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public override Guid? MessageId => _key.MessageId;

    public override bool ContinueProcessing { get; set; }

    public override bool IsMessageConsumed => false;

    public override bool IsOutboxDelivered => true;

    public override int ReceiveCount => _receiveCount;

    public override long? LastSequenceNumber => null;

    public override Task SetConsumedAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken.ThrowIfCancellationRequested();
        CancellationToken operationCancellationToken = ResolveOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();
        SerializedDurableSend[] messages;
        lock (_messagesLock)
            messages = _messages.ToArray();
        _store.CompleteConsumer(_key, _lease, messages, _limits, _timeProvider.GetUtcNow());
        return Task.CompletedTask;
    }

    public override Task SetDeliveredAsync(CancellationToken cancellationToken = default) =>
        CompletedOrCanceledAsync(ResolveOperationCancellationToken(cancellationToken));

    public override Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = ResolveOperationCancellationToken(cancellationToken);
        return operationCancellationToken.IsCancellationRequested
            ? Task.FromCanceled<List<OutboxMessageContext>>(operationCancellationToken)
            : Task.FromResult(new List<OutboxMessageContext>());
    }

    public override Task NotifyOutboxMessageDeliveredAsync(
        OutboxMessageContext message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return CompletedOrCanceledAsync(ResolveOperationCancellationToken(cancellationToken));
    }

    public override Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default) =>
        CompletedOrCanceledAsync(ResolveOperationCancellationToken(cancellationToken));

    public override Task AddSendAsync<TOutgoingMessage>(SendContext<TOutgoingMessage> context, CancellationToken cancellationToken = default)
        where TOutgoingMessage : class
    {
        ArgumentNullException.ThrowIfNull(context);
        CancellationToken.ThrowIfCancellationRequested();
        CancellationToken operationCancellationToken = cancellationToken.CanBeCanceled
            ? cancellationToken
            : context.CancellationToken.CanBeCanceled
                ? context.CancellationToken
                : CancellationToken;
        operationCancellationToken.ThrowIfCancellationRequested();
        if (context.MessageId is not { } messageId || messageId == Guid.Empty)
            throw new MessageException(typeof(TOutgoingMessage), "The SendContext MessageId must be present and nonempty");
        Uri destination = context.DestinationAddress
            ?? throw new MessageException(typeof(TOutgoingMessage), "The SendContext DestinationAddress must be present");
        DateTimeOffset now = _timeProvider.GetUtcNow();
        PayloadAdmissionRuntime<TBus>? admissionRuntime = _provider.GetService<PayloadAdmissionRuntime<TBus>>();
        if (admissionRuntime is null)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "In-memory reliable inbox",
                    typeof(TBus).ToString(),
                    "The payload-admission runtime is missing.",
                    "Register payload admission for this bus before using the reliable inbox"));
        }

        byte[] body = PayloadAdmissionTransportBoundary.Admit(admissionRuntime, context).ToArray();
        string contentType = context.ContentType?.ToString() ?? context.Serialization.DefaultContentType.ToString();
        if (!context.TryGetPayload(out PayloadAdmissionSerializationContext? admission)
            || !admission.TryCreateDurableProof(contentType, out DurablePayloadAdmissionProof proof)
            || !proof.MatchesEnvelope(body, contentType))
            throw new InvalidOperationException("The in-memory reliable inbox has no complete payload admission proof for its serialized envelope.");
        ReadOnlyMemory<byte> metadata = ReliableEnvelopeMetadataCodec.Capture(context, now, proof);
        if (context.MessageId != messageId)
            throw new MessageException(typeof(TOutgoingMessage), "The SendContext MessageId changed during serialization");

        var message = new SerializedDurableSend
        {
            Id = new DurableSendId(messageId),
            ContractIdentity = _contracts.GetIdentity(typeof(TOutgoingMessage)),
            DestinationAddress = destination,
            ContentType = contentType,
            Body = body,
            Metadata = metadata,
            MessageId = messageId,
            CorrelationId = context.CorrelationId,
            DueAt = context.Delay.HasValue ? now + context.Delay.Value : null,
        }.Validate();

        CancellationToken.ThrowIfCancellationRequested();
        operationCancellationToken.ThrowIfCancellationRequested();
        lock (_messagesLock)
            _messages.Add(message);
        return Task.CompletedTask;
    }

    static Task CompletedOrCanceledAsync(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.CompletedTask;

    CancellationToken ResolveOperationCancellationToken(CancellationToken cancellationToken) =>
        cancellationToken.CanBeCanceled ? cancellationToken : CancellationToken;
}
