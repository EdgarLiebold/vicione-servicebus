using System;
using System.Linq;

namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the receive fault event data.</summary>
public class ReceiveFaultEvent :
    ReceiveFault
{
    const int MaximumExceptionCount = 16;


    /// <summary>Initializes a new instance.</summary>
    public ReceiveFaultEvent()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="host">The host.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    /// <param name="faultedMessageId">The faulted message id.</param>
    /// <param name="faultMessageTypes">The fault message types.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
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

    /// <summary>Gets or sets the fault id.</summary>
    public Guid FaultId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the faulted message id.</summary>
    public Guid? FaultedMessageId { get; set; }
    /// <summary>Gets or sets the exceptions.</summary>
    public ExceptionInfo[] Exceptions { get; set; } = null!;
    /// <summary>Gets or sets the host.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the fault message types.</summary>
    public string[] FaultMessageTypes { get; set; } = null!;
    /// <summary>Gets or sets the content type.</summary>
    public string? ContentType { get; set; }
}
