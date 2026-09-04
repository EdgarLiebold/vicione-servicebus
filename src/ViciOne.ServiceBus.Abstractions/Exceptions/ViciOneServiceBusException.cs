using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class ViciOneServiceBusException :
    Exception
{
    public ViciOneServiceBusException()
    {
    }

    public ViciOneServiceBusException(string? message)
        : base(message)
    {
    }

    public ViciOneServiceBusException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
