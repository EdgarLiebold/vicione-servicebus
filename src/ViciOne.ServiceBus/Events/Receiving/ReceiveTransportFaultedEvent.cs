using System;

namespace ViciOne.ServiceBus.Events.Receiving;

/// <summary>Reports a recoverable or terminal receive-transport failure.</summary>
internal sealed class ReceiveTransportFaultedEvent :
    ReceiveTransportFaulted
{
    /// <summary>Creates a receive-transport fault notification.</summary>
    /// <param name="inputAddress">The address of the failed receive transport.</param>
    /// <param name="exception">The transport failure.</param>
    /// <param name="isTerminal">Whether the transport has exhausted recovery and will not retry.</param>
    public ReceiveTransportFaultedEvent(Uri inputAddress, Exception exception, bool isTerminal)
    {
        InputAddress = inputAddress ?? throw new ArgumentNullException(nameof(inputAddress));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        IsTerminal = isTerminal;
    }

    /// <summary>Gets the address of the failed receive transport.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets the transport failure.</summary>
    public Exception Exception { get; }
    /// <summary>Gets a value indicating whether the transport has exhausted recovery and will not retry.</summary>
    public bool IsTerminal { get; }
}
