using System;

namespace ViciOne.ServiceBus.Scheduling;

[Serializable]
public class ScheduleMessageCommand<T> :
    ScheduleMessage
    where T : class
{
    public ScheduleMessageCommand()
    {
    }

    public ScheduleMessageCommand(DateTimeOffset dueAt, Uri destination, T payload, Guid tokenId)
    {
        TokenId = tokenId;

        DueAt = dueAt.ToUniversalTime();

        Destination = destination;
        Payload = payload;

        PayloadType = MessageTypeCache<T>.MessageTypeNames;
    }

    public Guid TokenId { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public string[] PayloadType { get; set; } = null!;
    public Uri Destination { get; set; } = null!;
    public object Payload { get; set; } = null!;
}


[Serializable]
public class ScheduleMessageCommand :
    ScheduleMessage
{
    public Guid TokenId { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public string[] PayloadType { get; set; } = null!;
    public Uri Destination { get; set; } = null!;
    public object Payload { get; set; } = null!;
}
