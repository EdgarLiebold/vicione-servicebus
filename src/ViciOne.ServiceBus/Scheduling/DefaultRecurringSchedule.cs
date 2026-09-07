using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Provides identity, time-zone, and start-time defaults for a recurring schedule.</summary>
public abstract class DefaultRecurringSchedule :
    RecurringSchedule
{
    /// <summary>Initializes a valid recurring schedule from its expression, derived type, and clock.</summary>
    /// <param name="cronExpression">The cron expression that defines recurring occurrences.</param>
    /// <param name="description">An optional human-readable description; the schedule identifier is used when omitted.</param>
    /// <param name="timeProvider">The clock and local time zone used for the initial start time.</param>
    protected DefaultRecurringSchedule(string cronExpression, string? description = null, TimeProvider? timeProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        timeProvider ??= TimeProvider.System;

        ScheduleId = TypeCache.GetShortName(GetType());
        ScheduleGroup = GetType().Assembly.GetName().Name
            ?? throw new InvalidOperationException("The schedule assembly name is not available.");

        TimeZoneId = timeProvider.LocalTimeZone.Id;
        StartTime = timeProvider.GetLocalNow();
        CronExpression = cronExpression;
        Description = description ?? ScheduleId;
    }

    /// <summary>Gets the policy applied when a scheduled occurrence is missed.</summary>
    public MissedEventPolicy MisfirePolicy { get; protected set; }
    /// <summary>Gets the time-zone identifier used to evaluate the cron expression.</summary>
    public string TimeZoneId { get; protected set; }
    /// <summary>Gets the first time at which the schedule may run.</summary>
    public DateTimeOffset StartTime { get; protected set; }
    /// <summary>Gets the optional time after which the schedule no longer runs.</summary>
    public DateTimeOffset? EndTime { get; protected set; }
    /// <summary>Gets the identifier of the schedule within its group.</summary>
    public string ScheduleId { get; protected set; }
    /// <summary>Gets the group that scopes the schedule identifier.</summary>
    public string ScheduleGroup { get; protected set; }
    /// <summary>Gets the cron expression that defines recurring occurrences.</summary>
    public string CronExpression { get; protected set; }
    /// <summary>Gets a human-readable description of the schedule.</summary>
    public string Description { get; protected set; }
}
