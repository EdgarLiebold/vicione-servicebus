using System;
using System.Collections.Generic;
using System.Globalization;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.MessageJournal;

internal static class MessageJournalCaptureFactory
{
    public static MessageJournalCapture CreateSend<T>(
        SendContext<T> context,
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        Exception? exception)
        where T : class
    {
        MessageBody body = context is TransportSendContext transportContext
            ? transportContext.Body
            : context.Serializer.GetMessageBody(context);

        var metadata = CreateSendMetadata(context, exception);
        Add(metadata, MessageJournalMetadataKeys.ScheduledMessageId, context.ScheduledMessageId);
        Add(metadata, MessageJournalMetadataKeys.TimeToLive, context.TimeToLive);

        return new MessageJournalCapture(
            operation,
            outcome,
            context.ContentType?.ToString(),
            context.SupportedMessageTypes ?? [],
            metadata,
            SnapshotHeaders(context.Headers),
            body.ToArray());
    }

    public static MessageJournalCapture CreateConsume<T>(
        ConsumeContext<T> context,
        MessageJournalOutcome outcome,
        Exception? exception)
        where T : class
    {
        var metadata = CreateMessageMetadata(context, exception);
        Add(metadata, MessageJournalMetadataKeys.InputAddress, context.Advanced().ReceiveContext.InputAddress);
        Add(metadata, MessageJournalMetadataKeys.ExpiresAt, context.ExpirationTime);

        return new MessageJournalCapture(
            MessageJournalOperation.Consume,
            outcome,
            context.Advanced().ReceiveContext.ContentType?.ToString(),
            context.Advanced().SupportedMessageTypes,
            metadata,
            SnapshotHeaders(context.Headers),
            context.Advanced().ReceiveContext.Body.ToArray());
    }

    private static Dictionary<string, string> CreateMessageMetadata(MessageContext context, Exception? exception)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);

        Add(metadata, MessageJournalMetadataKeys.MessageId, context.MessageId);
        Add(metadata, MessageJournalMetadataKeys.RequestId, context.RequestId);
        Add(metadata, MessageJournalMetadataKeys.CorrelationId, context.CorrelationId);
        Add(metadata, MessageJournalMetadataKeys.ConversationId, context.ConversationId);
        Add(metadata, MessageJournalMetadataKeys.InitiatorId, context.InitiatorId);
        Add(metadata, MessageJournalMetadataKeys.SentAt, context.SentTime);
        Add(metadata, MessageJournalMetadataKeys.SourceAddress, context.SourceAddress);
        Add(metadata, MessageJournalMetadataKeys.DestinationAddress, context.DestinationAddress);
        Add(metadata, MessageJournalMetadataKeys.ResponseAddress, context.ResponseAddress);
        Add(metadata, MessageJournalMetadataKeys.FaultAddress, context.FaultAddress);

        if (exception is not null)
            metadata[MessageJournalMetadataKeys.FailureType] = TypeCache.GetShortName(exception.GetType());

        return metadata;
    }

    private static Dictionary<string, string> CreateSendMetadata(SendContext context, Exception? exception)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);

        Add(metadata, MessageJournalMetadataKeys.MessageId, context.MessageId);
        Add(metadata, MessageJournalMetadataKeys.RequestId, context.RequestId);
        Add(metadata, MessageJournalMetadataKeys.CorrelationId, context.CorrelationId);
        Add(metadata, MessageJournalMetadataKeys.ConversationId, context.ConversationId);
        Add(metadata, MessageJournalMetadataKeys.InitiatorId, context.InitiatorId);
        Add(metadata, MessageJournalMetadataKeys.SentAt, context.SentTime);
        Add(metadata, MessageJournalMetadataKeys.SourceAddress, context.SourceAddress);
        Add(metadata, MessageJournalMetadataKeys.DestinationAddress, context.DestinationAddress);
        Add(metadata, MessageJournalMetadataKeys.ResponseAddress, context.ResponseAddress);
        Add(metadata, MessageJournalMetadataKeys.FaultAddress, context.FaultAddress);

        if (exception is not null)
            metadata[MessageJournalMetadataKeys.FailureType] = TypeCache.GetShortName(exception.GetType());

        return metadata;
    }

    private static Dictionary<string, string> SnapshotHeaders(Headers headers)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach ((string key, object value) in headers.GetAll())
        {
            string? text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (text is not null)
                result[key] = text;
        }

        return result;
    }

    private static void Add(Dictionary<string, string> metadata, string key, Guid? value)
    {
        if (value.HasValue)
            metadata[key] = value.Value.ToString("D", CultureInfo.InvariantCulture);
    }

    private static void Add(Dictionary<string, string> metadata, string key, Uri? value)
    {
        if (value is not null)
            metadata[key] = value.AbsoluteUri;
    }

    private static void Add(Dictionary<string, string> metadata, string key, TimeSpan? value)
    {
        if (value.HasValue)
            metadata[key] = value.Value.ToString("c", CultureInfo.InvariantCulture);
    }

    private static void Add(Dictionary<string, string> metadata, string key, DateTimeOffset? value)
    {
        if (!value.HasValue)
            return;

        metadata[key] = value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }
}
