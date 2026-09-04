using System;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for scheduled recurring message.
/// </summary>
public interface ScheduledRecurringMessage
{
    /// <summary>
    /// Gets the schedule value.
    /// </summary>
    RecurringSchedule Schedule { get; }
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    Uri Destination { get; }
}


/// <summary>
/// Defines the contract for scheduled recurring message.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ScheduledRecurringMessage<out T> :
    ScheduledRecurringMessage
    where T : class
{
    /// <summary>
    /// Gets the payload value.
    /// </summary>
    T Payload { get; }
}
