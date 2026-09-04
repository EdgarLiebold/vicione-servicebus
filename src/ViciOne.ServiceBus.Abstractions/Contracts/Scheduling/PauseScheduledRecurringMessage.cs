using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Defines the contract for pause scheduled recurring message.
/// </summary>
public interface PauseScheduledRecurringMessage
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
