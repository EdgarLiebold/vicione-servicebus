using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Requests cancellation of a recurring schedule.</summary>
public sealed class CancelScheduledRecurringMessageCommand :
    CancelScheduledRecurringMessage
{
    /// <summary>Creates an empty command for deserialization.</summary>
    public CancelScheduledRecurringMessageCommand()
    {
    }

    /// <summary>Creates a recurring-schedule cancellation command.</summary>
    /// <param name="scheduleId">The schedule identifier.</param>
    /// <param name="scheduleGroup">The schedule group.</param>
    /// <param name="timestamp">The time at which cancellation was requested.</param>
    public CancelScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleGroup);

        Timestamp = timestamp;
        ScheduleId = scheduleId;
        ScheduleGroup = scheduleGroup;
    }

    /// <summary>Gets or sets the time at which cancellation was requested.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the schedule identifier.</summary>
    public string ScheduleId { get; set; } = null!;
    /// <summary>Gets or sets the schedule group.</summary>
    public string ScheduleGroup { get; set; } = null!;
}
