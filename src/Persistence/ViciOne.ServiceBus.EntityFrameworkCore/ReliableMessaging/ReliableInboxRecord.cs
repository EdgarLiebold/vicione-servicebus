namespace ViciOne.ServiceBus.EntityFrameworkCore;

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
