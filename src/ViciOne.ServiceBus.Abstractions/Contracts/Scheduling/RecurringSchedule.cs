using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Defines when and how a recurring message is delivered.</summary>
public interface RecurringSchedule
{
    /// <summary>Gets the time-zone identifier used to evaluate the cron expression.</summary>
    string TimeZoneId { get; }

    /// <summary>Gets the first time at which the schedule may run.</summary>
    DateTimeOffset StartTime { get; }

    /// <summary>Gets the optional time after which the schedule no longer runs.</summary>
    DateTimeOffset? EndTime { get; }

    /// <summary>Gets the identifier of the schedule within its group.</summary>
    string ScheduleId { get; }

    /// <summary>Gets the group that scopes the schedule identifier.</summary>
    string ScheduleGroup { get; }

    /// <summary>Gets the cron expression that defines recurring occurrences.</summary>
    string CronExpression { get; }

    /// <summary>Gets a human-readable description of the schedule.</summary>
    string Description { get; }

    /// <summary>Gets the misfire policy.</summary>
    MissedEventPolicy MisfirePolicy { get; }
}
