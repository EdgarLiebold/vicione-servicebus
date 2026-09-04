using System;

#nullable enable annotations
namespace ViciOne.ServiceBus.Scheduling;

public abstract class DefaultRecurringSchedule :
    RecurringSchedule
{
    protected DefaultRecurringSchedule(TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;

        ScheduleId = TypeCache.GetShortName(GetType());
        ScheduleGroup = GetType().Assembly.GetName().Name
            ?? throw new InvalidOperationException("The schedule assembly name is not available.");

        TimeZoneId = timeProvider.LocalTimeZone.Id;
        StartTime = timeProvider.GetLocalNow();
    }

    public MissedEventPolicy MisfirePolicy { get; protected set; }
    public string TimeZoneId { get; protected set; }
    public DateTimeOffset StartTime { get; protected set; }
    public DateTimeOffset? EndTime { get; protected set; }
    public string ScheduleId { get; protected set; }
    public string ScheduleGroup { get; protected set; }
    public string CronExpression { get; protected set; } = null!;
    public string Description { get; protected set; } = null!;
}
