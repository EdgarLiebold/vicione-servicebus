using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the receive transport faulted event data.</summary>
public class ReceiveTransportFaultedEvent :
    ReceiveTransportFaulted
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="inputAddress">The input address.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="isTerminal">The is terminal.</param>
    public ReceiveTransportFaultedEvent(Uri inputAddress, Exception exception, bool isTerminal)
    {
        InputAddress = inputAddress;
        Exception = exception;
        IsTerminal = isTerminal;
    }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets the exception.</summary>
    public Exception Exception { get; }
    /// <summary>Gets a value indicating whether terminal.</summary>
    public bool IsTerminal { get; }
}
