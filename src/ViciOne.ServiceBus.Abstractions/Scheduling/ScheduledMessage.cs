using System;

namespace ViciOne.ServiceBus;

public interface ScheduledMessage
{
    Guid TokenId { get; }
    DateTimeOffset DueAt { get; }
    Uri Destination { get; }
}


public interface ScheduledMessage<out T> :
    ScheduledMessage
    where T : class
{
    T Payload { get; }
}
