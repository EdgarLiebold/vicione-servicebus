using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;


namespace ViciOne.ServiceBus.Providers.Persistence;
/// <summary>Owns the application-to-persisted-intent transition for one typed bus.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
internal sealed class TypedDurableSender<TBus> : IDurableSender<TBus>
    where TBus : class, IBus
{
    private readonly IDurableSendAdmission<TBus> _admission;
    private readonly TBus _bus;
    private readonly IMessageContractCatalog _contractCatalog;
    private readonly PayloadAdmissionRuntime<TBus>? _payloadAdmission;

    public TypedDurableSender(
        TBus bus,
        IMessageContractCatalog contractCatalog,
        IDurableSendAdmission<TBus> admission,
        PayloadAdmissionRuntime<TBus>? payloadAdmission)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _contractCatalog = contractCatalog ?? throw new ArgumentNullException(nameof(contractCatalog));
        _admission = admission ?? throw new ArgumentNullException(nameof(admission));
        _payloadAdmission = payloadAdmission;
    }

    public Task<DurableSendReceipt> SendAsync<TMessage>(
        TMessage message,
        DurableSendOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class
        => SendAsync(EndpointConvention.GetDestinationAddress<TMessage>(_bus), message, options, cancellationToken);

    public async Task<DurableSendReceipt> SendAsync<TMessage>(
        Uri destinationAddress,
        TMessage message,
        DurableSendOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        OutgoingOptionsSnapshot? scheduledOptions = options.ScheduledMessageOptions is { } scheduled
            ? OutgoingOptionsSnapshot.Create(scheduled)
            : null;
        if (!destinationAddress.IsAbsoluteUri)
            throw new ArgumentException("A durable send destination must be an absolute URI.", nameof(destinationAddress));

        cancellationToken.ThrowIfCancellationRequested();
        MessageContractIdentity contractIdentity = _contractCatalog.GetIdentity(typeof(TMessage));
        ISendEndpoint endpoint = await _bus.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (endpoint is not ITransportSendEndpoint transportEndpoint)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"The send endpoint for bus '{typeof(TBus)}' does not expose the canonical transport send-context required by Durable Sender.", "Correct the named configuration before starting the host"));
        }

        SendContext<TMessage> context = await transportEndpoint
            .CreateSendContextAsync(message, Pipe.Empty<SendContext<TMessage>>(), cancellationToken)
            .ConfigureAwait(false);

        if (context is not MessageSendContext<TMessage> messageContext)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"The send context for bus '{typeof(TBus)}' cannot fix deterministic durable-admission metadata before serialization.", "Correct the named configuration before starting the host"));
        }
        messageContext.SetDurableAdmissionMetadata(options.IdempotencyKey.Value, options.CorrelationId);
        if (scheduledOptions is not null)
            OutgoingOptionsPipe.Apply(context, scheduledOptions);
        context.GetOrAddPayload(() => DurableSendEnvelopeMetadata.Instance);

        if (context is not TransportSendContext transportContext)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"The send context for bus '{typeof(TBus)}' is not a transport context and cannot be admitted by Durable Sender.", "Correct the named configuration before starting the host"));
        }

        if (_payloadAdmission is not null)
        {
            bool messageDataOffloadObserved = context.TryGetPayload(out MessageDataAdmissionEvidence? evidence)
                && evidence.HasStoredReference;
            context.GetOrAddPayload(() => new PayloadAdmissionSerializationContext(
                _payloadAdmission,
                messageDataOffloadObserved));
        }

        byte[] body = transportContext.Body.GetBytes();
        string contentType = context.ContentType?.ToString()
            ?? throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"The configured serializer for bus '{typeof(TBus)}' did not assign a content type.", "Correct the named configuration before starting the host"));
        ReadOnlyMemory<byte> metadata = scheduledOptions is null
            ? ReadOnlyMemory<byte>.Empty
            : ReliableEnvelopeMetadataCodec.Capture(context, options.DueAt ?? context.GetTimeProvider().GetUtcNow());
        var serialized = new SerializedDurableSend
        {
            Id = options.IdempotencyKey,
            ContractIdentity = contractIdentity,
            DestinationAddress = destinationAddress,
            ContentType = contentType,
            Body = body,
            Metadata = metadata,
            MessageId = context.MessageId,
            CorrelationId = context.CorrelationId,
            DueAt = options.DueAt,
        };

        DurableSendAdmissionResult result = await _admission
            .AdmitAsync(serialized, cancellationToken)
            .ConfigureAwait(false);
        return new DurableSendReceipt(result.Id, result.Disposition, result.StoredCount, result.StoredBytes);
    }
}
