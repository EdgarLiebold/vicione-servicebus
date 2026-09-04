using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a resume scheduled recurring message command implementation.
/// </summary>
public class ResumeScheduledRecurringMessageCommand :
    ResumeScheduledRecurringMessage
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ResumeScheduledRecurringMessageCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scheduleId">The schedule id value.</param>
    /// <param name="scheduleGroup">The schedule group value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    public ResumeScheduledRecurringMessageCommand(string scheduleId, string scheduleGroup, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;

        ScheduleId = scheduleId;
        ScheduleGroup = scheduleGroup;
    }

    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the schedule id value.
    /// </summary>
    public string ScheduleId { get; set; } = null!;
    /// <summary>
    /// Gets or sets the schedule group value.
    /// </summary>
    public string ScheduleGroup { get; set; } = null!;
}
