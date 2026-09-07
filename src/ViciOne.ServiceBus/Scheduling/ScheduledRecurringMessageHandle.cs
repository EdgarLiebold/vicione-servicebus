using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Identifies a scheduled recurring message and its original payload.</summary>
/// <typeparam name="T">The scheduled message type.</typeparam>
public sealed class ScheduledRecurringMessageHandle<T> :
    ScheduledRecurringMessage<T>
    where T : class
{
    /// <summary>Creates a recurring-message handle.</summary>
    /// <param name="schedule">The recurring schedule.</param>
    /// <param name="destination">The delivery destination.</param>
    /// <param name="payload">The original message payload.</param>
    public ScheduledRecurringMessageHandle(RecurringSchedule schedule, Uri destination, T payload)
    {
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    /// <summary>Gets the recurring schedule.</summary>
    public RecurringSchedule Schedule { get; }
    /// <summary>Gets the delivery destination.</summary>
    public Uri Destination { get; }
    /// <summary>Gets the original message payload.</summary>
    public T Payload { get; }
}
