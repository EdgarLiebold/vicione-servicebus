using System;

namespace ViciOne.ServiceBus.Scheduling;

public interface CancelScheduledRecurringMessage
{
    /// <summary>
    /// The date/time this message was created
    /// </summary>
    DateTime Timestamp { get; }

    string ScheduleId { get; }

    string ScheduleGroup { get; }
}
