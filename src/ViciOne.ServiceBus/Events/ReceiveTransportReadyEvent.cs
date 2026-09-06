using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the receive transport ready event data.</summary>
public class ReceiveTransportReadyEvent :
    ReceiveTransportReady
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="inputAddress">The input address.</param>
    /// <param name="isStarted">The is started.</param>
    public ReceiveTransportReadyEvent(Uri inputAddress, bool isStarted = true)
    {
        InputAddress = inputAddress;
        IsStarted = isStarted;
    }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets a value indicating whether started.</summary>
    public bool IsStarted { get; }
}
