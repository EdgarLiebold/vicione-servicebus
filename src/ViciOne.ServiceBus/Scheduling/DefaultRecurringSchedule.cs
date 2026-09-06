using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a default recurring schedule implementation.
/// </summary>
public abstract class DefaultRecurringSchedule :
    RecurringSchedule
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeProvider">The time provider value.</param>
    protected DefaultRecurringSchedule(TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;

        ScheduleId = TypeCache.GetShortName(GetType());
        ScheduleGroup = GetType().Assembly.GetName().Name
            ?? throw new InvalidOperationException("The schedule assembly name is not available.");

        TimeZoneId = timeProvider.LocalTimeZone.Id;
        StartTime = timeProvider.GetLocalNow();
    }

    /// <summary>
    /// Gets or sets the misfire policy value.
    /// </summary>
    public MissedEventPolicy MisfirePolicy { get; protected set; }
    /// <summary>
    /// Gets or sets the time zone id value.
    /// </summary>
    public string TimeZoneId { get; protected set; }
    /// <summary>
    /// Gets or sets the start time value.
    /// </summary>
    public DateTimeOffset StartTime { get; protected set; }
    /// <summary>
    /// Gets or sets the end time value.
    /// </summary>
    public DateTimeOffset? EndTime { get; protected set; }
    /// <summary>
    /// Gets or sets the schedule id value.
    /// </summary>
    public string ScheduleId { get; protected set; }
    /// <summary>
    /// Gets or sets the schedule group value.
    /// </summary>
    public string ScheduleGroup { get; protected set; }
    /// <summary>
    /// Gets or sets the cron expression value.
    /// </summary>
    public string CronExpression { get; protected set; } = null!;
    /// <summary>
    /// Gets or sets the description value.
    /// </summary>
    public string Description { get; protected set; } = null!;
}
