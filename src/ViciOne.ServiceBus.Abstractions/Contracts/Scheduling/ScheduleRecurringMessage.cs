// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Scheduling
{
    using System;


    public interface ScheduleRecurringMessage
    {
        Guid CorrelationId { get; }

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
}
