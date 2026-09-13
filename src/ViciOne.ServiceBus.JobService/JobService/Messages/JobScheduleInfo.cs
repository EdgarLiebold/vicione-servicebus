using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Scheduling;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable representation of a validated one-time or recurring job schedule.</summary>
internal sealed class JobScheduleInfo :
    IJobSchedule,
    IRecurringJobScheduleConfigurator,
    ISpecification
{
    /// <summary>Creates an empty schedule for configuration or deserialization.</summary>
    public JobScheduleInfo()
    {
    }

    /// <summary>Creates an isolated schedule snapshot from an incoming contract.</summary>
    /// <param name="schedule">The schedule contract to copy.</param>
    public JobScheduleInfo(IJobSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        CronExpression = schedule.CronExpression;
        TimeZoneId = schedule.TimeZoneId;
        Start = schedule.Start;
        End = schedule.End;
    }

    /// <summary>Validates the execution window and optional recurrence expression.</summary>
    /// <returns>All schedule validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        var hasCronExpression = !string.IsNullOrWhiteSpace(CronExpression);

        if (!hasCronExpression && Start.HasValue == false)
            yield return this.Failure("CronExpression", "must be specified when no start time is provided");

        if (Start.HasValue && End.HasValue && Start.Value > End.Value)
            yield return this.Failure("Start", "must be earlier than or equal to End");

        if (TimeZoneId is not null && string.IsNullOrWhiteSpace(TimeZoneId))
            yield return this.Failure("TimeZoneId", "must not be empty when specified");

        if (!hasCronExpression)
            yield break;

        ValidationResult? failure = null;
        try
        {
            _ = new CronExpression(CronExpression);
        }
        catch (FormatException exception)
        {
            failure = this.Failure("CronExpression", $"is invalid: {exception.Message}");
        }

        if (failure != null)
            yield return failure;
    }

    /// <summary>Gets or sets the cron expression for recurring execution.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Gets or sets the time-zone identifier used to evaluate <see cref="CronExpression" />.</summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Gets or sets the earliest permitted execution time.</summary>
    public DateTimeOffset? Start { get; set; }

    /// <summary>Gets or sets the last permitted execution time.</summary>
    public DateTimeOffset? End { get; set; }
}
