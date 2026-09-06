using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Controls the lifetime of scheduled recurring message.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ScheduledRecurringMessageHandle<T> :
    ScheduledRecurringMessage<T>
    where T : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="schedule">The schedule.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="payload">The payload.</param>
    public ScheduledRecurringMessageHandle(RecurringSchedule schedule, Uri destination, T payload)
    {
        Schedule = schedule;
        Destination = destination;
        Payload = payload;
    }

    /// <summary>Gets or sets the schedule.</summary>
    public RecurringSchedule Schedule { get; private set; }
    /// <summary>Gets or sets the destination.</summary>
    public Uri Destination { get; private set; }
    /// <summary>Gets or sets the payload.</summary>
    public T Payload { get; private set; }
}
