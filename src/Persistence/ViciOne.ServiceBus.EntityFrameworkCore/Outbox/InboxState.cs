using System;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Persists duplicate-detection and receive-side outbox progress for one message/consumer pair.</summary>
public class InboxState
{
    /// <summary>Gets or sets the surrogate primary key used for ordered relational storage.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the identifier of the incoming message.</summary>
    public Guid MessageId { get; set; }

    /// <summary>Gets or sets the stable identifier of the endpoint/consumer combination.</summary>
    public Guid ConsumerId { get; set; }

    /// <summary>Gets or sets the token written when the row is claimed in a transaction.</summary>
    public Guid LockId { get; set; }

    /// <summary>Gets or sets the EF Core concurrency token for the row.</summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>Gets or sets the UTC time when the message was first received.</summary>
    public DateTimeOffset Received { get; set; }

    /// <summary>Gets or sets the number of receive attempts recorded for this key.</summary>
    public int ReceiveCount { get; set; }

    /// <summary>Gets or sets the message expiration time captured from the incoming envelope.</summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>Gets or sets the UTC time when consumer processing completed successfully.</summary>
    public DateTimeOffset? Consumed { get; set; }

    /// <summary>Gets or sets the UTC time when all outgoing messages were sent to their transports.</summary>
    public DateTimeOffset? Delivered { get; set; }

    /// <summary>Gets or sets the highest outgoing sequence number sent successfully.</summary>
    public long? LastSequenceNumber { get; set; }
}
