using System;

namespace ViciOne.ServiceBus.Scheduling;

public class ScheduledMessageHandle<T> :
    ScheduledMessage<T>
    where T : class
{
    public ScheduledMessageHandle(Guid tokenId, DateTimeOffset dueAt, Uri destination, T payload)
    {
        TokenId = tokenId;
        DueAt = dueAt;
        Destination = destination;
        Payload = payload;
    }

    public Guid TokenId { get; }
    public DateTimeOffset DueAt { get; }
    public Uri Destination { get; }
    public T Payload { get; }
}
