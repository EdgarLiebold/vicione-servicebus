namespace ViciOne.ServiceBus
{
    using System;


    public interface ReceiveTransportFaulted :
        ReceiveTransportEvent
    {
        Exception? Exception { get; }
    }
}
