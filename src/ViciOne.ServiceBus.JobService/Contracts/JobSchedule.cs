using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Describes when a submitted job may run and, optionally, how it recurs.</summary>
public interface JobSchedule
{
    /// <summary>Gets the cron expression for a recurring job, or <see langword="null" /> for a one-time job.</summary>
    string? CronExpression { get; }

    /// <summary>Gets the time-zone identifier used to evaluate <see cref="CronExpression" />, or <see langword="null" /> for UTC.</summary>
    string? TimeZoneId { get; }

    /// <summary>Gets the earliest permitted execution time, or <see langword="null" /> to start at the next eligible instant.</summary>
    DateTimeOffset? Start { get; }

    /// <summary>Gets the last permitted execution time, or <see langword="null" /> when the schedule has no end.</summary>
    DateTimeOffset? End { get; }
}
