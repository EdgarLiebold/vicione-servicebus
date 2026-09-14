using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Events.Faults;

/// <summary>Materializes the fault contract published for a message that failed during consumption.</summary>
/// <typeparam name="TMessage">The faulted message type.</typeparam>
internal sealed class FaultEvent<TMessage> :
    Fault<TMessage>
{
    const int MaximumExceptionCount = 16;

    /// <summary>Creates an empty instance for contract materialization.</summary>
    public FaultEvent()
    {
    }

    /// <summary>Creates a bounded fault snapshot from a local exception.</summary>
    /// <param name="message">The message whose consumption failed.</param>
    /// <param name="faultedMessageId">The identifier of the failed message, when supplied.</param>
    /// <param name="host">The host that consumed the failed message.</param>
    /// <param name="exception">The consumer or pipeline failure.</param>
    /// <param name="faultMessageTypes">The message type identifiers declared by the failed envelope.</param>
    /// <param name="timeProvider">The time source used to timestamp the fault.</param>
    public FaultEvent(TMessage message, Guid? faultedMessageId, HostInfo host, Exception exception, string[] faultMessageTypes,
        TimeProvider? timeProvider = null)
        : this(message, faultedMessageId, host, FaultExceptionInfo.CreateMany(exception), faultMessageTypes, timeProvider)
    {
    }

    /// <summary>Creates a bounded fault snapshot from existing exception contracts.</summary>
    /// <param name="message">The message whose consumption failed.</param>
    /// <param name="faultedMessageId">The identifier of the failed message, when supplied.</param>
    /// <param name="host">The host that consumed the failed message.</param>
    /// <param name="exceptions">The exception snapshots to include, in reporting order.</param>
    /// <param name="faultMessageTypes">The message type identifiers declared by the failed envelope.</param>
    /// <param name="timeProvider">The time source used to timestamp the fault.</param>
    public FaultEvent(TMessage message, Guid? faultedMessageId, HostInfo host, IEnumerable<ExceptionInfo> exceptions, string[] faultMessageTypes,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(exceptions);
        ArgumentNullException.ThrowIfNull(faultMessageTypes);

        Timestamp = (timeProvider ?? TimeProvider.System).GetUtcNow();
        FaultId = NewId.NextGuid();

        Message = message;
        Host = host;
        FaultMessageTypes = faultMessageTypes
            .Select(messageType => messageType
                ?? throw new ArgumentException("Fault message type collections cannot contain null elements.", nameof(faultMessageTypes)))
            .ToArray();
        FaultedMessageId = faultedMessageId;

        Exceptions = exceptions
            .Take(MaximumExceptionCount)
            .Select(exceptionInfo => exceptionInfo
                ?? throw new ArgumentException("Fault exception collections cannot contain null elements.", nameof(exceptions)))
            .ToArray();
    }

    /// <summary>Gets or sets the identifier of this fault event.</summary>
    public Guid FaultId { get; set; }

    /// <summary>Gets or sets the identifier of the failed message, when supplied.</summary>
    public Guid? FaultedMessageId { get; set; }

    /// <summary>Gets or sets the UTC time at which the fault was created.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Gets or sets the bounded exception snapshots associated with the fault.</summary>
    public ExceptionInfo[] Exceptions { get; set; } = null!;

    /// <summary>Gets or sets the host that consumed the failed message.</summary>
    public HostInfo Host { get; set; } = null!;

    /// <summary>Gets or sets the message type identifiers declared by the failed envelope.</summary>
    public string[] FaultMessageTypes { get; set; } = null!;

    /// <summary>Gets or sets the message whose consumption failed.</summary>
    public TMessage Message { get; set; } = default!;

}

/// <summary>Materializes the non-generic fault contract during serialization.</summary>
internal sealed class FaultEvent :
    Fault
{
    /// <summary>Gets or sets the identifier of this fault event.</summary>
    public Guid FaultId { get; set; }

    /// <summary>Gets or sets the identifier of the failed message, when supplied.</summary>
    public Guid? FaultedMessageId { get; set; }

    /// <summary>Gets or sets the UTC time at which the fault was created.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Gets or sets the bounded exception snapshots associated with the fault.</summary>
    public ExceptionInfo[] Exceptions { get; set; } = null!;

    /// <summary>Gets or sets the host that consumed the failed message.</summary>
    public HostInfo Host { get; set; } = null!;

    /// <summary>Gets or sets the message type identifiers declared by the failed envelope.</summary>
    public string[] FaultMessageTypes { get; set; } = null!;
}
