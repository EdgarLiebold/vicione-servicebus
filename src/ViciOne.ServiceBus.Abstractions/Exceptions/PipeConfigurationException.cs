using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class PipeConfigurationException :
    ViciOneServiceBusException
{
    public PipeConfigurationException()
    {
    }

    public PipeConfigurationException(string message)
        : base(message)
    {
    }

    public PipeConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
