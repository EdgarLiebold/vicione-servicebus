using System;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the operations required by scheduled recurring message.</summary>
public interface ScheduledRecurringMessage
{
    /// <summary>Gets the schedule.</summary>
    RecurringSchedule Schedule { get; }
    /// <summary>Gets the destination.</summary>
    Uri Destination { get; }
}


/// <summary>Defines the operations required by scheduled recurring message.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ScheduledRecurringMessage<out T> :
    ScheduledRecurringMessage
    where T : class
{
    /// <summary>Gets the payload.</summary>
    T Payload { get; }
}
