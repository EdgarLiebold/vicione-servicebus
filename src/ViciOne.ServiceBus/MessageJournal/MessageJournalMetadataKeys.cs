#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>
/// Stable keys exposed to journal policies. A policy explicitly chooses which values may persist.
/// </summary>
public static class MessageJournalMetadataKeys
{
    public const string MessageId = "message.id";
    public const string RequestId = "message.request_id";
    public const string CorrelationId = "message.correlation_id";
    public const string ConversationId = "message.conversation_id";
    public const string InitiatorId = "message.initiator_id";
    public const string ScheduledMessageId = "message.scheduled_id";
    public const string SentAt = "message.sent_at";
    public const string ExpiresAt = "message.expires_at";
    public const string TimeToLive = "message.time_to_live";
    public const string SourceAddress = "messaging.source.address";
    public const string DestinationAddress = "messaging.destination.address";
    public const string InputAddress = "messaging.input.address";
    public const string ResponseAddress = "messaging.response.address";
    public const string FaultAddress = "messaging.fault.address";
    public const string FailureType = "failure.type";
}
