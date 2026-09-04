using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class AmazonSqsTransportException :
    ViciOneServiceBusException
{
    public AmazonSqsTransportException()
    {
    }

    public AmazonSqsTransportException(string message)
        : base(message)
    {
    }

    public AmazonSqsTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
