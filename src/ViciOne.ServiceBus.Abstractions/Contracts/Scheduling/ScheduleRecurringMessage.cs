using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Defines the contract for schedule recurring message.
/// </summary>
public interface ScheduleRecurringMessage
{
    /// <summary>
    /// Gets the schedule value.
    /// </summary>
    RecurringSchedule Schedule { get; }

    /// <summary>
    /// The message types implemented by the message
    /// </summary>
    string[] PayloadType { get; }

    /// <summary>
    /// The destination where the message should be sent
    /// </summary>
    Uri Destination { get; }

    /// <summary>
    /// The actual scheduled message payload
    /// </summary>
    object Payload { get; }
}
