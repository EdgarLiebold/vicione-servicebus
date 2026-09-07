namespace ViciOne.ServiceBus.EntityFrameworkCore;

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
