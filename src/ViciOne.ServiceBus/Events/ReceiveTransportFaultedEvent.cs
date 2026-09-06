using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a receive transport faulted event implementation.
/// </summary>
public class ReceiveTransportFaultedEvent :
    ReceiveTransportFaulted
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="isTerminal">The is terminal value.</param>
    public ReceiveTransportFaultedEvent(Uri inputAddress, Exception exception, bool isTerminal)
    {
        InputAddress = inputAddress;
        Exception = exception;
        IsTerminal = isTerminal;
    }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress { get; }

    /// <summary>
    /// Gets the exception value.
    /// </summary>
    public Exception Exception { get; }
    /// <summary>
    /// Gets the is terminal value.
    /// </summary>
    public bool IsTerminal { get; }
}
