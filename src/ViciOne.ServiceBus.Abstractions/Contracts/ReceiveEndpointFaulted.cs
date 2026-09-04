using System;

namespace ViciOne.ServiceBus;

public interface ReceiveEndpointFaulted :
    ReceiveEndpointEvent
{
    Exception Exception { get; }
    bool IsTerminal { get; }
}
