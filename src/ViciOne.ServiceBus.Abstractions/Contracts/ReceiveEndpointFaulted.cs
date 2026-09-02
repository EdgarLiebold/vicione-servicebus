namespace ViciOne.ServiceBus
{
    using System;


    public interface ReceiveEndpointFaulted :
        ReceiveEndpointEvent
    {
        Exception Exception { get; }
        bool IsTerminal { get; }
    }
}
