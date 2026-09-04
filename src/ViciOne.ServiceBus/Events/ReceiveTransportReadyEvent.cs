using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a receive transport ready event implementation.
/// </summary>
public class ReceiveTransportReadyEvent :
    ReceiveTransportReady
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="isStarted">The is started value.</param>
    public ReceiveTransportReadyEvent(Uri inputAddress, bool isStarted = true)
    {
        InputAddress = inputAddress;
        IsStarted = isStarted;
    }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress { get; }

    /// <summary>
    /// Gets the is started value.
    /// </summary>
    public bool IsStarted { get; }
}
