using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class AmazonSqsTransportConfigurationException :
    AmazonSqsTransportException
{
    public AmazonSqsTransportConfigurationException()
    {
    }

    public AmazonSqsTransportConfigurationException(string message)
        : base(message)
    {
    }

    public AmazonSqsTransportConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
