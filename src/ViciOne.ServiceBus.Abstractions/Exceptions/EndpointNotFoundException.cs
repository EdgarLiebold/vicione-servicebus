using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class EndpointNotFoundException :
    ViciOneServiceBusException
{
    public EndpointNotFoundException()
    {
    }

    public EndpointNotFoundException(string message)
        : base(message)
    {
    }

    public EndpointNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
