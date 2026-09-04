using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class PipelineException :
    ViciOneServiceBusException
{
    public PipelineException()
    {
    }

    public PipelineException(string message)
        : base(message)
    {
    }

    public PipelineException(string message, Exception innerException)
        :
        base(message, innerException)
    {
    }
}
