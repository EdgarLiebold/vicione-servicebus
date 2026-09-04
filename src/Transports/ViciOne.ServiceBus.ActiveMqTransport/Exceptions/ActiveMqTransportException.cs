using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class ActiveMqTransportException :
    ViciOneServiceBusException
{
    public ActiveMqTransportException()
    {
    }

    public ActiveMqTransportException(string message)
        : base(message)
    {
    }

    public ActiveMqTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
