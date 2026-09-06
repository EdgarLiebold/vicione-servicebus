using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the fault event data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class FaultEvent<T> :
    Fault<T>
{
    const int MaximumExceptionCount = 16;


    /// <summary>Initializes a new instance.</summary>
    public FaultEvent()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="faultedMessageId">The faulted message id.</param>
    /// <param name="host">The host.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="faultMessageTypes">The fault message types.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public FaultEvent(T message, Guid? faultedMessageId, HostInfo host, Exception exception, string[] faultMessageTypes,
        TimeProvider? timeProvider = null)
        : this(message, faultedMessageId, host, GetExceptions(exception), faultMessageTypes, timeProvider)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="faultedMessageId">The faulted message id.</param>
    /// <param name="host">The host.</param>
    /// <param name="exceptions">The exceptions.</param>
    /// <param name="faultMessageTypes">The fault message types.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
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

    /// <summary>Gets or sets the fault id.</summary>
    public Guid FaultId { get; set; }
    /// <summary>Gets or sets the faulted message id.</summary>
    public Guid? FaultedMessageId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the exceptions.</summary>
    public ExceptionInfo[] Exceptions { get; set; } = null!;
    /// <summary>Gets or sets the host.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the fault message types.</summary>
    public string[] FaultMessageTypes { get; set; } = null!;
    /// <summary>Gets or sets the message.</summary>
    public T Message { get; set; } = default!;
    static ExceptionInfo[] GetExceptions(Exception exception)
    {
        var aggregateException = exception as AggregateException;

        return aggregateException?.InnerExceptions.Where(x => x != null).Take(MaximumExceptionCount)
            .Select(ExceptionInfo (x) => new FaultExceptionInfo(x)).ToArray()
            ?? [new FaultExceptionInfo(exception)];
    }
}


/// <summary>Carries the fault event data.</summary>
public class FaultEvent :
    Fault
{
    /// <summary>Gets or sets the fault id.</summary>
    public Guid FaultId { get; set; }
    /// <summary>Gets or sets the faulted message id.</summary>
    public Guid? FaultedMessageId { get; set; }
    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the exceptions.</summary>
    public ExceptionInfo[] Exceptions { get; set; } = null!;
    /// <summary>Gets or sets the host.</summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>Gets or sets the fault message types.</summary>
    public string[] FaultMessageTypes { get; set; } = null!;
}
