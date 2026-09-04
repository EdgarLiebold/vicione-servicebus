using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Scheduling;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a recurring job schedule info implementation.
/// </summary>
public class RecurringJobScheduleInfo :
    RecurringJobSchedule,
    IRecurringJobScheduleConfigurator,
    ISpecification
{
    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        var hasCronExpression = !string.IsNullOrWhiteSpace(CronExpression);

        if (!hasCronExpression && Start.HasValue == false)
            yield return this.Failure("CronExpression", "must be specified");

        if (Start.HasValue && End.HasValue && Start.Value > End.Value)
            yield return this.Failure("Start", "must be <= End");

        if (!hasCronExpression)
            yield break;

        ValidationResult? failure = null;
        try
        {
            _ = new CronExpression(CronExpression);
        }
        catch (FormatException exception)
        {
            failure = this.Failure("CronExpression", $"Is invalid: {exception.Message}");
        }

        if (failure != null)
            yield return failure;
    }

    /// <summary>
    /// Gets or sets the cron expression value.
    /// </summary>
    public string? CronExpression { get; set; }
    /// <summary>
    /// Gets or sets the time zone id value.
    /// </summary>
    public string? TimeZoneId { get; set; }
    /// <summary>
    /// Gets or sets the start value.
    /// </summary>
    public DateTimeOffset? Start { get; set; }
    /// <summary>
    /// Gets or sets the end value.
    /// </summary>
    public DateTimeOffset? End { get; set; }
}
