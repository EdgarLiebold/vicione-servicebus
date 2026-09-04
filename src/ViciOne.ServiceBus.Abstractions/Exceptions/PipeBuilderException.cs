using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class PipeFactoryException :
    ViciOneServiceBusException
{
    public PipeFactoryException()
    {
    }

    public PipeFactoryException(string message)
        : base(message)
    {
    }

    public PipeFactoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
