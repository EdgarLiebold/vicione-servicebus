namespace ViciOne.ServiceBus.Scheduling
{
    using System;


    public interface PauseScheduledRecurringMessage
    {
        /// <summary>
        /// The date/time this message was created
        /// </summary>
        DateTime Timestamp { get; }

        string ScheduleId { get; }

        string ScheduleGroup { get; }
    }
}
