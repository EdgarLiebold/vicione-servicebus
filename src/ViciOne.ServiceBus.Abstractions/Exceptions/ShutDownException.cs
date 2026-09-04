using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class ShutDownException :
    ViciOneServiceBusException
{
    public ShutDownException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
