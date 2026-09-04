using System;

namespace ViciOne.ServiceBus;

public interface ReceiveTransportFaulted :
    ReceiveTransportEvent
{
    Exception Exception { get; }
    bool IsTerminal { get; }
}
