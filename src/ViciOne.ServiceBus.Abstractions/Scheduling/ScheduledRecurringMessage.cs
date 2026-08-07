// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using Scheduling;


    public interface ScheduledRecurringMessage
    {
        RecurringSchedule Schedule { get; }
        Uri Destination { get; }
    }


    public interface ScheduledRecurringMessage<out T> :
        ScheduledRecurringMessage
        where T : class
    {
        T Payload { get; }
    }
}
