using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Defines the contract for resume scheduled recurring message.
/// </summary>
public interface ResumeScheduledRecurringMessage
{
    /// <summary>
    /// The date/time this message was created
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the schedule id value.
    /// </summary>
    string ScheduleId { get; }

    /// <summary>
    /// Gets the schedule group value.
    /// </summary>
    string ScheduleGroup { get; }
}
