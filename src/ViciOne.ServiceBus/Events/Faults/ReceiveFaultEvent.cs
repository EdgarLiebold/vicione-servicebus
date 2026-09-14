using System;
using System.Linq;

namespace ViciOne.ServiceBus.Events.Faults;

/// <summary>Materializes the fault contract published when an incoming envelope cannot be consumed.</summary>
internal sealed class ReceiveFaultEvent :
    ReceiveFault
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public ReceiveFaultEvent()
    {
    }

    /// <summary>Creates a bounded fault snapshot from a receive-pipeline failure.</summary>
    /// <param name="host">The host that received the failed envelope.</param>
    /// <param name="exception">The receive-pipeline failure.</param>
    /// <param name="contentType">The content type declared by the incoming envelope.</param>
    /// <param name="faultedMessageId">The identifier of the failed message, when supplied.</param>
    /// <param name="faultMessageTypes">The message type identifiers declared by the failed envelope.</param>
    /// <param name="timeProvider">The time source used to timestamp the fault.</param>
    public ReceiveFaultEvent(HostInfo host, Exception exception, string? contentType, Guid? faultedMessageId, string[]? faultMessageTypes,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(exception);

        Timestamp = (timeProvider ?? TimeProvider.System).GetUtcNow();
        FaultId = NewId.NextGuid();

        Host = host;
        ContentType = contentType;
        FaultedMessageId = faultedMessageId;
        FaultMessageTypes = faultMessageTypes is null
            ? []
            : faultMessageTypes.Select(messageType => messageType
                ?? throw new ArgumentException("Fault message type collections cannot contain null elements.", nameof(faultMessageTypes))).ToArray();

        Exceptions = FaultExceptionInfo.CreateMany(exception);
    }

    /// <summary>Gets or sets the identifier of this fault event.</summary>
    public Guid FaultId { get; set; }

    /// <summary>Gets or sets the UTC time at which the fault was created.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Gets or sets the identifier of the failed message, when supplied.</summary>
    public Guid? FaultedMessageId { get; set; }

    /// <summary>Gets or sets the bounded exception snapshots associated with the fault.</summary>
    public ExceptionInfo[] Exceptions { get; set; } = null!;

    /// <summary>Gets or sets the host that received the failed envelope.</summary>
    public HostInfo Host { get; set; } = null!;

    /// <summary>Gets or sets the message type identifiers declared by the failed envelope.</summary>
    public string[] FaultMessageTypes { get; set; } = null!;

    /// <summary>Gets or sets the content type declared by the incoming envelope.</summary>
    public string? ContentType { get; set; }
}
