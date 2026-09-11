using System;

namespace ViciOne.ServiceBus.Events.Receiving;

/// <summary>Reports that a receive transport is available to accept deliveries.</summary>
internal sealed class ReceiveTransportReadyEvent :
    ReceiveTransportReady
{
    /// <summary>Creates a receive-transport readiness notification.</summary>
    /// <param name="inputAddress">The transport's input address.</param>
    /// <param name="isStarted">Whether this notification follows transport startup.</param>
    public ReceiveTransportReadyEvent(Uri inputAddress, bool isStarted = true)
    {
        InputAddress = inputAddress ?? throw new ArgumentNullException(nameof(inputAddress));
        IsStarted = isStarted;
    }

    /// <summary>Gets the transport's input address.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets a value indicating whether this notification follows transport startup.</summary>
    public bool IsStarted { get; }
}
