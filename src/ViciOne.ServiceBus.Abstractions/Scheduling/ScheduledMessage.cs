// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface ScheduledMessage
    {
        Guid TokenId { get; }
        DateTime ScheduledTime { get; }
        Uri Destination { get; }
    }


    public interface ScheduledMessage<out T> :
        ScheduledMessage
        where T : class
    {
        T Payload { get; }
    }
}
