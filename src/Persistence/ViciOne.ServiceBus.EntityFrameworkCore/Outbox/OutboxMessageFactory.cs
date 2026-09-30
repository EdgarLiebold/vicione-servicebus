using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal static class OutboxMessageFactory
{
    internal static OutboxMessage CreateAdmitted<T>(
        SendContext<T> context,
        IPayloadAdmissionRuntime runtime,
        IObjectDeserializer deserializer,
        TimeProvider timeProvider,
        Guid? inboxMessageId = null,
        Guid? inboxConsumerId = null,
        Guid? outboxId = null)
        where T : class
    {
        TransportBodyMaterializer.MetadataSnapshot expected = TransportBodyMaterializer.CaptureExpectedMetadata(context);
        try
        {
            PayloadAdmissionTransportBoundary.Admit(runtime, context);
            return TransportBodyMaterializer.ReadWithExpectedMetadata(context,
                guardedBody => Create(context, deserializer, timeProvider, inboxMessageId, inboxConsumerId,
                    outboxId, guardedBody), expected);
        }
        catch (Exception failure)
        {
            if (expected.ChangedField(context) is not null)
            {
                expected.Restore(context);
                TransportBodyMaterializer.MarkMutationFailure(failure);
            }
            throw;
        }
    }

    public static OutboxMessage Create<T>(
        SendContext<T> context,
        IObjectDeserializer deserializer,
        TimeProvider timeProvider,
        Guid? inboxMessageId = null,
        Guid? inboxConsumerId = null,
        Guid? outboxId = null,
        MessageBody? admittedBody = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(deserializer);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (context.MessageId is not { } messageId || messageId == Guid.Empty)
            throw new MessageException(typeof(T), "The SendContext MessageId must be present and nonempty");

        ValidateOwner(inboxMessageId, inboxConsumerId, outboxId);

        MessageBody body = admittedBody ?? context.Serializer.GetMessageBody(context);
        string contentType = context.ContentType?.ToString() ?? context.Serialization.DefaultContentType.ToString();
        DurablePayloadAdmissionProof? proof = admittedBody is null
            ? null
            : RequireAdmissionProof(context, body, contentType);
        string transportBody = body.GetRequiredTransportText();
        if (context.MessageId != messageId)
            throw new MessageException(typeof(T), "The SendContext MessageId changed during serialization");

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        var outboxMessage = new OutboxMessage
        {
            MessageId = messageId,
            ConversationId = context.ConversationId,
            CorrelationId = context.CorrelationId,
            InitiatorId = context.InitiatorId,
            RequestId = context.RequestId,
            SourceAddress = context.SourceAddress,
            DestinationAddress = context.DestinationAddress,
            ResponseAddress = context.ResponseAddress,
            FaultAddress = context.FaultAddress,
            SentTime = context.SentTime ?? now,
            ContentType = contentType,
            MessageType = string.Join(";", context.SupportedMessageTypes),
            Body = transportBody,
            InboxMessageId = inboxMessageId,
            InboxConsumerId = inboxConsumerId,
            OutboxId = outboxId,
        };

        if (context.TimeToLive.HasValue)
            outboxMessage.ExpirationTime = now + context.TimeToLive;

        if (context.Delay.HasValue)
            outboxMessage.EnqueueTime = now + context.Delay;

        string? headers = deserializer.SerializeDictionary(context.Headers.GetAll());
        outboxMessage.Headers = proof is { } durableProof
            ? OutboxAdmissionMetadata.Encode(headers, durableProof)
            : headers;

        if (context is TransportSendContext<T> transportSendContext)
        {
            var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            transportSendContext.WritePropertiesTo(properties);
            outboxMessage.Properties = deserializer.SerializeDictionary(properties);
        }

        return outboxMessage;
    }

    static DurablePayloadAdmissionProof RequireAdmissionProof<T>(SendContext<T> context, MessageBody body, string contentType)
        where T : class
    {
        if (!context.TryGetPayload(out PayloadAdmissionSerializationContext? admission)
            || !admission.TryCreateDurableProof(contentType, out DurablePayloadAdmissionProof proof)
            || !proof.MatchesEnvelope(body.ToArray(), contentType))
        {
            throw new InvalidOperationException(
                "The EF outbox has no complete payload admission proof for its serialized envelope.");
        }

        return proof;
    }

    static void ValidateOwner(Guid? inboxMessageId, Guid? inboxConsumerId, Guid? outboxId)
    {
        if (inboxMessageId == Guid.Empty)
            throw new ArgumentException("The inbox message identifier must be nonempty.", nameof(inboxMessageId));
        if (inboxConsumerId == Guid.Empty)
            throw new ArgumentException("The inbox consumer identifier must be nonempty.", nameof(inboxConsumerId));
        if (outboxId == Guid.Empty)
            throw new ArgumentException("The transactional outbox identifier must be nonempty.", nameof(outboxId));

        bool hasInboxOwner = inboxMessageId.HasValue && inboxConsumerId.HasValue;
        if (inboxMessageId.HasValue != inboxConsumerId.HasValue)
            throw new ArgumentException("Inbox ownership requires both the message and consumer identifiers.");
        if (hasInboxOwner == outboxId.HasValue)
            throw new ArgumentException("An outbox message must have exactly one inbox or transactional-outbox owner.");
    }
}
