using System;


namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class DurableSendRecord
{
    public required string StoreKey { get; set; }
    public Guid Id { get; set; }
    public Guid GenerationToken { get; set; }
    public required string ContractIdentity { get; set; }
    public required string DestinationAddress { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Body { get; set; }
    public byte[]? Metadata { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? CorrelationId { get; set; }
    public long StorageSize { get; set; }
    public DurableSendStatus Status { get; set; }
    public DateTime EnqueuedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public int DeliveryAttempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public DateTime? QuarantinedAt { get; set; }
    public DurableSendFailureKind LastFailureKind { get; set; }
    public string? LastFailureType { get; set; }
    public DateTime? LastFailureAt { get; set; }
}

internal sealed class DurableSendCapacityState
{
    public required string StoreKey { get; set; }
    public int StoredCount { get; set; }
    public long StoredBytes { get; set; }
}

internal sealed class ReliableInboxRecord
{
    public required string StoreKey { get; set; }
    public Guid MessageId { get; set; }
    public Guid ConsumerId { get; set; }
    public ReliableInboxStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? QuarantinedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureType { get; set; }
}

internal sealed class ReliableRecurringScheduleRecord
{
    public required string StoreKey { get; set; }
    public Guid ScheduleId { get; set; }
    public required string CronExpression { get; set; }
    public required string TimeZoneId { get; set; }
    public DateTime NextDueAt { get; set; }
    public bool IsPaused { get; set; }
    public DateTime CreatedAt { get; set; }
}
