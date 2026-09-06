using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a fault event implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class FaultEvent<T> :
    Fault<T>
{
    const int MaximumExceptionCount = 16;


    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public FaultEvent()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="faultedMessageId">The faulted message id value.</param>
    /// <param name="host">The host value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="faultMessageTypes">The fault message types value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public FaultEvent(T message, Guid? faultedMessageId, HostInfo host, Exception exception, string[] faultMessageTypes,
        TimeProvider? timeProvider = null)
        : this(message, faultedMessageId, host, GetExceptions(exception), faultMessageTypes, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="faultedMessageId">The faulted message id value.</param>
    /// <param name="host">The host value.</param>
    /// <param name="exceptions">The exceptions value.</param>
    /// <param name="faultMessageTypes">The fault message types value.</param>
    /// <param name="timeProvider">The time provider value.</param>
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

    /// <summary>
    /// Gets or sets the fault id value.
    /// </summary>
    public Guid FaultId { get; set; }
    /// <summary>
    /// Gets or sets the faulted message id value.
    /// </summary>
    public Guid? FaultedMessageId { get; set; }
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the exceptions value.
    /// </summary>
    public ExceptionInfo[] Exceptions { get; set; } = null!;
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>
    /// Gets or sets the fault message types value.
    /// </summary>
    public string[] FaultMessageTypes { get; set; } = null!;
    /// <summary>
    /// Gets or sets the message value.
    /// </summary>
    public T Message { get; set; } = default!;
    static ExceptionInfo[] GetExceptions(Exception exception)
    {
        var aggregateException = exception as AggregateException;

        return aggregateException?.InnerExceptions.Where(x => x != null).Take(MaximumExceptionCount)
            .Select(ExceptionInfo (x) => new FaultExceptionInfo(x)).ToArray()
            ?? [new FaultExceptionInfo(exception)];
    }
}


/// <summary>
/// Provides a fault event implementation.
/// </summary>
public class FaultEvent :
    Fault
{
    /// <summary>
    /// Gets or sets the fault id value.
    /// </summary>
    public Guid FaultId { get; set; }
    /// <summary>
    /// Gets or sets the faulted message id value.
    /// </summary>
    public Guid? FaultedMessageId { get; set; }
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the exceptions value.
    /// </summary>
    public ExceptionInfo[] Exceptions { get; set; } = null!;
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public HostInfo Host { get; set; } = null!;
    /// <summary>
    /// Gets or sets the fault message types value.
    /// </summary>
    public string[] FaultMessageTypes { get; set; } = null!;
}
