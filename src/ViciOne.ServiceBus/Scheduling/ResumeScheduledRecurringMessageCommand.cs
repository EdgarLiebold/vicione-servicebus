using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Requests resumption of a recurring schedule.</summary>
public sealed class ResumeScheduledRecurringMessageCommand :
    ResumeScheduledRecurringMessage
{
    /// <summary>Creates an empty command for deserialization.</summary>
    public ResumeScheduledRecurringMessageCommand()
    {
    }

    /// <summary>Creates a recurring-schedule resumption command.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="scheduleGroup">The schedule group.</param>
    /// <param name="timestamp">The time at which resumption was requested.</param>
    public ResumeScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleGroup);

        Timestamp = timestamp;
        ScheduleId = scheduleId;
        ScheduleGroup = scheduleGroup;
    }

    /// <summary>Gets or sets the time at which resumption was requested.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the schedule identifier.</summary>
    public string ScheduleId { get; set; } = null!;
    /// <summary>Gets or sets the schedule group.</summary>
    public string ScheduleGroup { get; set; } = null!;
}
