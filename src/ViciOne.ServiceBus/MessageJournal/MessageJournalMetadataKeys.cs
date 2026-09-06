namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>
/// Stable keys exposed to journal policies. A policy explicitly chooses which values may persist.
/// </summary>
public static class MessageJournalMetadataKeys
{
    /// <summary>
    /// Defines the message id value.
    /// </summary>
    public const string MessageId = "message.id";
    /// <summary>
    /// Defines the request id value.
    /// </summary>
    public const string RequestId = "message.request_id";
    /// <summary>
    /// Defines the correlation id value.
    /// </summary>
    public const string CorrelationId = "message.correlation_id";
    /// <summary>
    /// Defines the conversation id value.
    /// </summary>
    public const string ConversationId = "message.conversation_id";
    /// <summary>
    /// Defines the initiator id value.
    /// </summary>
    public const string InitiatorId = "message.initiator_id";
    /// <summary>
    /// Defines the scheduled message id value.
    /// </summary>
    public const string ScheduledMessageId = "message.scheduled_id";
    /// <summary>
    /// Defines the sent at value.
    /// </summary>
    public const string SentAt = "message.sent_at";
    /// <summary>
    /// Defines the expires at value.
    /// </summary>
    public const string ExpiresAt = "message.expires_at";
    /// <summary>
    /// Defines the time to live value.
    /// </summary>
    public const string TimeToLive = "message.time_to_live";
    /// <summary>
    /// Defines the source address value.
    /// </summary>
    public const string SourceAddress = "messaging.source.address";
    /// <summary>
    /// Defines the destination address value.
    /// </summary>
    public const string DestinationAddress = "messaging.destination.address";
    /// <summary>
    /// Defines the input address value.
    /// </summary>
    public const string InputAddress = "messaging.input.address";
    /// <summary>
    /// Defines the response address value.
    /// </summary>
    public const string ResponseAddress = "messaging.response.address";
    /// <summary>
    /// Defines the fault address value.
    /// </summary>
    public const string FaultAddress = "messaging.fault.address";
    /// <summary>
    /// Defines the failure type value.
    /// </summary>
    public const string FailureType = "failure.type";
}
