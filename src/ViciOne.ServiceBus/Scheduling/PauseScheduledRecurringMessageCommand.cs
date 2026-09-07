using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Requests suspension of a recurring schedule.</summary>
public sealed class PauseScheduledRecurringMessageCommand :
    PauseScheduledRecurringMessage
{
    /// <summary>Creates an empty command for deserialization.</summary>
    public PauseScheduledRecurringMessageCommand()
    {
    }

    /// <summary>Creates a recurring-schedule suspension command.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="scheduleGroup">The schedule group.</param>
    /// <param name="timestamp">The time at which suspension was requested.</param>
    public PauseScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleGroup);

        Timestamp = timestamp;
        ScheduleId = scheduleId;
        ScheduleGroup = scheduleGroup;
    }

    /// <summary>Gets or sets the time at which suspension was requested.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the schedule identifier.</summary>
    public string ScheduleId { get; set; } = null!;
    /// <summary>Gets or sets the schedule group.</summary>
    public string ScheduleGroup { get; set; } = null!;
}
