using System;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Persistent state for one ordered bus-outbox sequence. The bus identity is persisted so multiple bus instances
/// can safely share the same DbContext and physical outbox tables without cross-delivery.
/// </summary>
public class OutboxState
{
    /// <summary>
    /// Gets or sets the outbox id value.
    /// </summary>
    public Guid OutboxId { get; set; }
    /// <summary>
    /// Gets or sets the bus key value.
    /// </summary>
    public string BusKey { get; set; } = null!;
    /// <summary>
    /// Gets or sets the lock id value.
    /// </summary>
    public Guid LockId { get; set; }
    /// <summary>
    /// Gets or sets the row version value.
    /// </summary>
    public byte[]? RowVersion { get; set; }
    /// <summary>
    /// Gets or sets the created value.
    /// </summary>
    public DateTimeOffset Created { get; set; }
    /// <summary>
    /// Gets or sets the status value.
    /// </summary>
    public OutboxDeliveryStatus Status { get; set; }
    /// <summary>
    /// Gets or sets the next delivery time value.
    /// </summary>
    public DateTimeOffset? NextDeliveryTime { get; set; }
    /// <summary>
    /// Gets or sets the delivery attempts value.
    /// </summary>
    public int DeliveryAttempts { get; set; }
    /// <summary>
    /// Gets or sets the last failure kind value.
    /// </summary>
    public OutboxFailureKind LastFailureKind { get; set; }
    /// <summary>
    /// Gets or sets the last failure time value.
    /// </summary>
    public DateTimeOffset? LastFailureTime { get; set; }
    /// <summary>
    /// Gets or sets the last failure value.
    /// </summary>
    public string? LastFailure { get; set; }
    /// <summary>
    /// Gets or sets the failed sequence number value.
    /// </summary>
    public long? FailedSequenceNumber { get; set; }
    /// <summary>
    /// Gets or sets the failed message id value.
    /// </summary>
    public Guid? FailedMessageId { get; set; }
    /// <summary>
    /// Gets or sets the delivered value.
    /// </summary>
    public DateTimeOffset? Delivered { get; set; }
    /// <summary>
    /// Gets or sets the last sequence number value.
    /// </summary>
    public long? LastSequenceNumber { get; set; }
}
