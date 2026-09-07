using System;

namespace ViciOne.ServiceBus;

/// <summary>Describes the fault produced when a consumer cannot process a message.</summary>
/// <typeparam name="T">The faulted message contract type.</typeparam>
public interface Fault<out T> :
    Fault
{
    /// <summary>Gets the message whose consumption failed.</summary>
    T Message { get; }
}


/// <summary>
/// Describes fault metadata published after message processing fails, or returned directly for a request.
/// </summary>
public interface Fault
{
    /// <summary>Gets the unique identifier assigned to the fault.</summary>
    Guid FaultId { get; }

    /// <summary>Gets the identifier of the failed message, when the incoming envelope supplied one.</summary>
    Guid? FaultedMessageId { get; }

    /// <summary>Gets the UTC time at which the fault was created.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the bounded exception snapshots associated with the failure.</summary>
    ExceptionInfo[] Exceptions { get; }

    /// <summary>Gets information about the host that observed the failure.</summary>
    HostInfo Host { get; }

    /// <summary>Gets the message type identifiers declared by the failed envelope.</summary>
    string[] FaultMessageTypes { get; }
}
