namespace ViciOne.ServiceBus;

using System;


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
