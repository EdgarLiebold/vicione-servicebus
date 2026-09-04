using System;
using System.Linq;

namespace ViciOne.ServiceBus.Events;

[Serializable]
public class ReceiveFaultEvent :
    ReceiveFault
{
    const int MaximumExceptionCount = 16;


    public ReceiveFaultEvent()
    {
    }

    public ReceiveFaultEvent(HostInfo host, Exception exception, string? contentType, Guid? faultedMessageId, string[]? faultMessageTypes,
        TimeProvider? timeProvider = null)
    {
        Timestamp = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        FaultId = NewId.NextGuid();

        Host = host;
        ContentType = contentType;
        FaultedMessageId = faultedMessageId;
        FaultMessageTypes = faultMessageTypes ?? [];

        var aggregateException = exception as AggregateException;

        Exceptions = aggregateException?.InnerExceptions.Take(MaximumExceptionCount)
            .Select(ExceptionInfo (x) => new FaultExceptionInfo(x)).ToArray()
            ?? [new FaultExceptionInfo(exception)];
    }

    public Guid FaultId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public Guid? FaultedMessageId { get; set; }
    public ExceptionInfo[] Exceptions { get; set; } = null!;
    public HostInfo Host { get; set; } = null!;
    public string[] FaultMessageTypes { get; set; } = null!;
    public string? ContentType { get; set; }
}
