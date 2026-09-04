using System;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

/// <summary>
/// Persistent state for one ordered bus-outbox sequence. The bus identity is persisted so multiple bus instances
/// can safely share the same DbContext and physical outbox tables without cross-delivery.
/// </summary>
public class OutboxState
{
    public Guid OutboxId { get; set; }
    public string BusKey { get; set; } = null!;
    public Guid LockId { get; set; }
    public byte[]? RowVersion { get; set; }
    public DateTimeOffset Created { get; set; }
    public OutboxDeliveryStatus Status { get; set; }
    public DateTimeOffset? NextDeliveryTime { get; set; }
    public int DeliveryAttempts { get; set; }
    public OutboxFailureKind LastFailureKind { get; set; }
    public DateTimeOffset? LastFailureTime { get; set; }
    public string? LastFailure { get; set; }
    public long? FailedSequenceNumber { get; set; }
    public Guid? FailedMessageId { get; set; }
    public DateTimeOffset? Delivered { get; set; }
    public long? LastSequenceNumber { get; set; }
}
