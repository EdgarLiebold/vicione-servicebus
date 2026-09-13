using System;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Configures the recurrence rule and active window of a scheduled job.</summary>
public interface IRecurringJobScheduleConfigurator
{
    /// <summary>Sets the cron expression that defines recurring execution.</summary>
    string CronExpression { set; }

    /// <summary>Sets the earliest instant at which the recurring job may run.</summary>
    DateTimeOffset? Start { set; }

    /// <summary>Sets the last instant at which the recurring job may run.</summary>
    DateTimeOffset? End { set; }

    /// <summary>Sets the time-zone identifier used to evaluate the cron expression, or <see langword="null" /> for UTC.</summary>
    string? TimeZoneId { set; }
}
