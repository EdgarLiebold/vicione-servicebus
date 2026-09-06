using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Defines the operations required by pause scheduled recurring message.</summary>
public interface PauseScheduledRecurringMessage
{
    /// <summary>The date/time this message was created.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the schedule id.</summary>
    string ScheduleId { get; }

    /// <summary>Gets the schedule group.</summary>
    string ScheduleGroup { get; }
}
