using System;
using System.Linq;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a receive fault event implementation.
/// </summary>
[Serializable]
public class ReceiveFaultEvent :
    ReceiveFault
{
    const int MaximumExceptionCount = 16;


    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ReceiveFaultEvent()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="host">The host value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="contentType">The content type value.</param>
    /// <param name="faultedMessageId">The faulted message id value.</param>
    /// <param name="faultMessageTypes">The fault message types value.</param>
    /// <param name="timeProvider">The time provider value.</param>
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

    /// <summary>
    /// Gets or sets the fault id value.
    /// </summary>
    public Guid FaultId { get; set; }
    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the faulted message id value.
    /// </summary>
    public Guid? FaultedMessageId { get; set; }
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
    /// Gets or sets the content type value.
    /// </summary>
    public string? ContentType { get; set; }
}
