using System;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Persistent state for one ordered bus-outbox sequence. The bus identity is persisted so multiple bus instances
/// can safely share the same DbContext and physical outbox tables without cross-delivery.
/// </summary>
public class OutboxState
{
    /// <summary>Gets or sets the identifier shared by an ordered group of outgoing messages.</summary>
    public Guid OutboxId { get; set; }
    /// <summary>Gets or sets the persistent bus identity that owns this outbox.</summary>
    public string BusKey { get; set; } = null!;
    /// <summary>Gets or sets the token written when a delivery worker claims the row.</summary>
    public Guid LockId { get; set; }
    /// <summary>Gets or sets the EF Core concurrency token for the row.</summary>
    public byte[]? RowVersion { get; set; }
    /// <summary>Gets or sets the UTC creation time used to order outbox work.</summary>
    public DateTimeOffset Created { get; set; }
    /// <summary>Gets or sets the current delivery state.</summary>
    public OutboxDeliveryStatus Status { get; set; }
    /// <summary>Gets or sets the UTC time at which a scheduled retry becomes eligible.</summary>
    public DateTimeOffset? NextDeliveryTime { get; set; }
    /// <summary>Gets or sets the number of consecutive failed delivery attempts.</summary>
    public int DeliveryAttempts { get; set; }
    /// <summary>Gets or sets the classification of the most recent failure.</summary>
    public OutboxFailureKind LastFailureKind { get; set; }
    /// <summary>Gets or sets the UTC time of the most recent failure.</summary>
    public DateTimeOffset? LastFailureTime { get; set; }
    /// <summary>Gets or sets the bounded diagnostic text for the most recent failure.</summary>
    public string? LastFailure { get; set; }
    /// <summary>Gets or sets the sequence number of the message that failed.</summary>
    public long? FailedSequenceNumber { get; set; }
    /// <summary>Gets or sets the identifier of the message that failed.</summary>
    public Guid? FailedMessageId { get; set; }
    /// <summary>Gets or sets the UTC time when all messages in the outbox were sent.</summary>
    public DateTimeOffset? Delivered { get; set; }
    /// <summary>Gets or sets the highest sequence number sent successfully.</summary>
    public long? LastSequenceNumber { get; set; }
}
