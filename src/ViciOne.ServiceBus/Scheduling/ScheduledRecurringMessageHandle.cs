using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a scheduled recurring message handle implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ScheduledRecurringMessageHandle<T> :
    ScheduledRecurringMessage<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="payload">The payload value.</param>
    public ScheduledRecurringMessageHandle(RecurringSchedule schedule, Uri destination, T payload)
    {
        Schedule = schedule;
        Destination = destination;
        Payload = payload;
    }

    /// <summary>
    /// Gets or sets the schedule value.
    /// </summary>
    public RecurringSchedule Schedule { get; private set; }
    /// <summary>
    /// Gets or sets the destination value.
    /// </summary>
    public Uri Destination { get; private set; }
    /// <summary>
    /// Gets or sets the payload value.
    /// </summary>
    public T Payload { get; private set; }
}
