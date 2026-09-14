namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>Stable keys exposed to journal policies. A policy explicitly chooses which values may persist.</summary>
public static class MessageJournalMetadataKeys
{
    /// <summary>Identifies the observed message envelope.</summary>
    public const string MessageId = "message.id";
    /// <summary>Identifies the request/response interaction associated with the message.</summary>
    public const string RequestId = "message.request_id";
    /// <summary>Identifies the application correlation chain associated with the message.</summary>
    public const string CorrelationId = "message.correlation_id";
    /// <summary>Identifies the conversation spanning related messages.</summary>
    public const string ConversationId = "message.conversation_id";
    /// <summary>Identifies the message that initiated the current conversation.</summary>
    public const string InitiatorId = "message.initiator_id";
    /// <summary>Identifies the scheduler-owned message instance.</summary>
    public const string ScheduledMessageId = "message.scheduled_id";
    /// <summary>Records the UTC time carried by the message as its send time.</summary>
    public const string SentAt = "message.sent_at";
    /// <summary>Records the UTC time after which a consumed message is expired.</summary>
    public const string ExpiresAt = "message.expires_at";
    /// <summary>Records the requested lifetime of an outgoing message.</summary>
    public const string TimeToLive = "message.time_to_live";
    /// <summary>Records the logical source address carried by the message.</summary>
    public const string SourceAddress = "messaging.source.address";
    /// <summary>Records the logical destination address of the message.</summary>
    public const string DestinationAddress = "messaging.destination.address";
    /// <summary>Records the receive endpoint that accepted the message.</summary>
    public const string InputAddress = "messaging.input.address";
    /// <summary>Records the address to which a response should be sent.</summary>
    public const string ResponseAddress = "messaging.response.address";
    /// <summary>Records the address to which a fault should be sent.</summary>
    public const string FaultAddress = "messaging.fault.address";
    /// <summary>Identifies the concrete exception type reported for a faulted operation.</summary>
    public const string FailureType = "failure.type";
}
