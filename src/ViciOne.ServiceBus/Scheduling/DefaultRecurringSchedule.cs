using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Defines the schedule for default recurring.</summary>
public abstract class DefaultRecurringSchedule :
    RecurringSchedule
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeProvider">The time source used by the operation.</param>
    protected DefaultRecurringSchedule(TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;

        ScheduleId = TypeCache.GetShortName(GetType());
        ScheduleGroup = GetType().Assembly.GetName().Name
            ?? throw new InvalidOperationException("The schedule assembly name is not available.");

        TimeZoneId = timeProvider.LocalTimeZone.Id;
        StartTime = timeProvider.GetLocalNow();
    }

    /// <summary>Gets or sets the misfire policy.</summary>
    public MissedEventPolicy MisfirePolicy { get; protected set; }
    /// <summary>Gets or sets the time zone id.</summary>
    public string TimeZoneId { get; protected set; }
    /// <summary>Gets or sets the start time.</summary>
    public DateTimeOffset StartTime { get; protected set; }
    /// <summary>Gets or sets the end time.</summary>
    public DateTimeOffset? EndTime { get; protected set; }
    /// <summary>Gets or sets the schedule id.</summary>
    public string ScheduleId { get; protected set; }
    /// <summary>Gets or sets the schedule group.</summary>
    public string ScheduleGroup { get; protected set; }
    /// <summary>Gets or sets the cron expression.</summary>
    public string CronExpression { get; protected set; } = null!;
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; protected set; } = null!;
}
