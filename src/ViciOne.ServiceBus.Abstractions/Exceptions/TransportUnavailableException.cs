using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class TransportUnavailableException :
    ViciOneServiceBusException
{
    public TransportUnavailableException()
    {
    }

    public TransportUnavailableException(string message)
        : base(message)
    {
    }

    public TransportUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
