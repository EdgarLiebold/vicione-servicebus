using System;

namespace ViciOne.ServiceBus.Events;

public class ReceiveTransportReadyEvent :
    ReceiveTransportReady
{
    public ReceiveTransportReadyEvent(Uri inputAddress, bool isStarted = true)
    {
        InputAddress = inputAddress;
        IsStarted = isStarted;
    }

    public Uri InputAddress { get; }

    public bool IsStarted { get; }
}
