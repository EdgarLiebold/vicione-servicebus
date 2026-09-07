using System;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Identifies a recurring schedule and its delivery destination.</summary>
public interface ScheduledRecurringMessage
{
    /// <summary>Gets the recurring schedule.</summary>
    RecurringSchedule Schedule { get; }

    /// <summary>Gets the delivery destination.</summary>
    Uri Destination { get; }
}

/// <summary>Identifies a recurring schedule and its original message payload.</summary>
/// <typeparam name="T">The scheduled message type.</typeparam>
public interface ScheduledRecurringMessage<out T> :
    ScheduledRecurringMessage
    where T : class
{
    /// <summary>Gets the original message payload.</summary>
    T Payload { get; }
}
