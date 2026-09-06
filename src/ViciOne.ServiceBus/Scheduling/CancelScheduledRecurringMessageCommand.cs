using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Carries the command for cancel scheduled recurring message.</summary>
public class CancelScheduledRecurringMessageCommand :
    CancelScheduledRecurringMessage
{
    /// <summary>Initializes a new instance.</summary>
    public CancelScheduledRecurringMessageCommand()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scheduleId">The schedule id.</param>
    /// <param name="scheduleGroup">The schedule group.</param>
    /// <param name="timestamp">The timestamp.</param>
    public CancelScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;

        ScheduleId = scheduleId;
        ScheduleGroup = scheduleGroup;
    }

    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the schedule id.</summary>
    public string ScheduleId { get; set; } = null!;
    /// <summary>Gets or sets the schedule group.</summary>
    public string ScheduleGroup { get; set; } = null!;
}
