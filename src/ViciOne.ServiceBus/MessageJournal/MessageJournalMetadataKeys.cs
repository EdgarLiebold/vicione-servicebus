namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>Stable keys exposed to journal policies. A policy explicitly chooses which values may persist.</summary>
public static class MessageJournalMetadataKeys
{
    /// <summary>Exposes the message id used by the containing type.</summary>
    public const string MessageId = "message.id";
    /// <summary>Exposes the request id used by the containing type.</summary>
    public const string RequestId = "message.request_id";
    /// <summary>Exposes the correlation id used by the containing type.</summary>
    public const string CorrelationId = "message.correlation_id";
    /// <summary>Exposes the conversation id used by the containing type.</summary>
    public const string ConversationId = "message.conversation_id";
    /// <summary>Exposes the initiator id used by the containing type.</summary>
    public const string InitiatorId = "message.initiator_id";
    /// <summary>Exposes the scheduled message id used by the containing type.</summary>
    public const string ScheduledMessageId = "message.scheduled_id";
    /// <summary>Exposes the sent at used by the containing type.</summary>
    public const string SentAt = "message.sent_at";
    /// <summary>Exposes the expires at used by the containing type.</summary>
    public const string ExpiresAt = "message.expires_at";
    /// <summary>Exposes the time to live used by the containing type.</summary>
    public const string TimeToLive = "message.time_to_live";
    /// <summary>Exposes the source address used by the containing type.</summary>
    public const string SourceAddress = "messaging.source.address";
    /// <summary>Exposes the destination address used by the containing type.</summary>
    public const string DestinationAddress = "messaging.destination.address";
    /// <summary>Exposes the input address used by the containing type.</summary>
    public const string InputAddress = "messaging.input.address";
    /// <summary>Exposes the response address used by the containing type.</summary>
    public const string ResponseAddress = "messaging.response.address";
    /// <summary>Exposes the fault address used by the containing type.</summary>
    public const string FaultAddress = "messaging.fault.address";
    /// <summary>Exposes the failure type used by the containing type.</summary>
    public const string FailureType = "failure.type";
}
