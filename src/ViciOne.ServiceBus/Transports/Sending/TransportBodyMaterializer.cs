using System;
using System.Net.Mime;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.Transports;

internal static class TransportBodyMaterializer
{
    private static readonly object MutationMarker = new();

    internal static MessageException CreateMutationFailure<T>(string changedField) where T : class
    {
        var failure = new MessageException(typeof(T), $"The SendContext {changedField} changed during serialization");
        failure.Data[MutationMarker] = true;
        return failure;
    }

    internal static void MarkMutationFailure(Exception failure) => failure.Data[MutationMarker] = true;

    public static bool IsMutationFailure(Exception exception) =>
        exception.Data[MutationMarker] is true;

    public static byte[] ToArray<T>(SendContext<T> context) where T : class =>
        Materialize(context, static body => body.ToArray());

    public static string GetTransportText<T>(SendContext<T> context) where T : class =>
        Materialize(context, static body => body.GetRequiredTransportText());

    internal static TResult Read<T, TResult>(SendContext<T> context, Func<MessageBody, TResult> readBody)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(readBody);
        MetadataSnapshot metadata = CaptureExpectedMetadata(context);
        MessageSendContext<T>? messageContext = context as MessageSendContext<T>;
        if (metadata.ChangedField(context) is { } staleField)
        {
            metadata.Restore(context);
            throw CreateMutationFailure<T>(staleField);
        }
        if (messageContext?.MessageTypesChanged == true)
        {
            messageContext.RestoreSerializedMessageTypes();
            throw CreateMutationFailure<T>(nameof(SendContext.SupportedMessageTypes));
        }
        TResult result;
        try
        {
            MessageBody body = (context as TransportSendContext)?.Body
                ?? throw new InvalidOperationException("A transport send context with a serialized body is required.");
            result = readBody(body);
        }
        catch (Exception failure)
        {
            if (metadata.ChangedField(context) is not null)
            {
                metadata.Restore(context);
                MarkMutationFailure(failure);
            }
            if (messageContext?.MessageTypesChanged == true)
            {
                messageContext.RestoreSerializedMessageTypes();
                MarkMutationFailure(failure);
            }

            throw;
        }

        if (metadata.ChangedField(context) is { } changedField)
        {
            metadata.Restore(context);
            throw CreateMutationFailure<T>(changedField);
        }
        if (messageContext?.MessageTypesChanged == true)
        {
            messageContext.RestoreSerializedMessageTypes();
            throw CreateMutationFailure<T>(nameof(SendContext.SupportedMessageTypes));
        }

        return result;
    }

    private static TResult Materialize<T, TResult>(SendContext<T> context, Func<MessageBody, TResult> readBody)
        where T : class => Read(context, readBody);

    internal static MetadataSnapshot CaptureExpectedMetadata<T>(SendContext<T> context) where T : class =>
        context is MessageSendContext<T> messageContext && messageContext.SerializedMetadata is { } bound
            ? bound
            : MetadataSnapshot.Capture(context);

    internal readonly record struct MetadataSnapshot(
        Guid? MessageId,
        Guid? RequestId,
        Guid? CorrelationId,
        Guid? ConversationId,
        Guid? InitiatorId,
        Guid? ScheduledMessageId,
        Uri? SourceAddress,
        Uri? DestinationAddress,
        Uri? ResponseAddress,
        Uri? FaultAddress,
        TimeSpan? TimeToLive,
        string? ContentType)
    {
        public static MetadataSnapshot Capture(SendContext context) => new(
            context.MessageId,
            context.RequestId,
            context.CorrelationId,
            context.ConversationId,
            context.InitiatorId,
            context.ScheduledMessageId,
            context.SourceAddress,
            context.DestinationAddress,
            context.ResponseAddress,
            context.FaultAddress,
            context.TimeToLive,
            context.ContentType?.ToString());

        public string? ChangedField(SendContext context)
        {
            if (context.MessageId != MessageId) return nameof(MessageId);
            if (context.RequestId != RequestId) return nameof(RequestId);
            if (context.CorrelationId != CorrelationId) return nameof(CorrelationId);
            if (context.ConversationId != ConversationId) return nameof(ConversationId);
            if (context.InitiatorId != InitiatorId) return nameof(InitiatorId);
            if (context.ScheduledMessageId != ScheduledMessageId) return nameof(ScheduledMessageId);
            if (!Equals(context.SourceAddress, SourceAddress)) return nameof(SourceAddress);
            if (!Equals(context.DestinationAddress, DestinationAddress)) return nameof(DestinationAddress);
            if (!Equals(context.ResponseAddress, ResponseAddress)) return nameof(ResponseAddress);
            if (!Equals(context.FaultAddress, FaultAddress)) return nameof(FaultAddress);
            if (context.TimeToLive != TimeToLive) return nameof(TimeToLive);
            if (!string.Equals(context.ContentType?.ToString(), ContentType, StringComparison.Ordinal)) return nameof(ContentType);
            return null;
        }

        public void Restore(SendContext context)
        {
            context.MessageId = MessageId;
            context.RequestId = RequestId;
            context.CorrelationId = CorrelationId;
            context.ConversationId = ConversationId;
            context.InitiatorId = InitiatorId;
            context.ScheduledMessageId = ScheduledMessageId;
            context.SourceAddress = SourceAddress;
            context.DestinationAddress = DestinationAddress;
            context.ResponseAddress = ResponseAddress;
            context.FaultAddress = FaultAddress;
            context.TimeToLive = TimeToLive;
            context.ContentType = ContentType is null ? null : new ContentType(ContentType);
        }
    }
}
