namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Identifies the stable, payload-free reason for a transactional-outbox delivery failure.</summary>
public enum OutboxFailureCode
{
    /// <summary>No failure is recorded.</summary>
    None = 0,

    /// <summary>The destination transport rejected or failed to deliver the message.</summary>
    TransportSendFailed = 1,

    /// <summary>The persisted message metadata could not be deserialized.</summary>
    MetadataDeserializationFailed = 2,

    /// <summary>The persisted message has no destination address.</summary>
    MissingDestinationAddress = 3,

    /// <summary>The persisted delivery-attempt counter is outside its valid range.</summary>
    InvalidDeliveryAttemptCount = 4,
}
