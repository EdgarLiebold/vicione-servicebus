using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Events;

[Serializable]
public class FaultEvent<T> :
    Fault<T>
{
    const int MaximumExceptionCount = 16;


    public FaultEvent()
    {
    }

    public FaultEvent(T message, Guid? faultedMessageId, HostInfo host, Exception exception, string[] faultMessageTypes,
        TimeProvider? timeProvider = null)
        : this(message, faultedMessageId, host, GetExceptions(exception), faultMessageTypes, timeProvider)
    {
    }

    public FaultEvent(T message, Guid? faultedMessageId, HostInfo host, IEnumerable<ExceptionInfo> exceptions, string[] faultMessageTypes,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(exceptions);

        Timestamp = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        FaultId = NewId.NextGuid();

        Message = message;
        Host = host;
        FaultMessageTypes = faultMessageTypes;
        FaultedMessageId = faultedMessageId;

        Exceptions = exceptions.Take(MaximumExceptionCount).ToArray();
    }

    public Guid FaultId { get; set; }
    public Guid? FaultedMessageId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public ExceptionInfo[] Exceptions { get; set; } = null!;
    public HostInfo Host { get; set; } = null!;
    public string[] FaultMessageTypes { get; set; } = null!;
    public T Message { get; set; } = default!;
    static ExceptionInfo[] GetExceptions(Exception exception)
    {
        var aggregateException = exception as AggregateException;

        return aggregateException?.InnerExceptions.Where(x => x != null).Take(MaximumExceptionCount)
            .Select(ExceptionInfo (x) => new FaultExceptionInfo(x)).ToArray()
            ?? [new FaultExceptionInfo(exception)];
    }
}


[Serializable]
public class FaultEvent :
    Fault
{
    public Guid FaultId { get; set; }
    public Guid? FaultedMessageId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public ExceptionInfo[] Exceptions { get; set; } = null!;
    public HostInfo Host { get; set; } = null!;
    public string[] FaultMessageTypes { get; set; } = null!;
}
