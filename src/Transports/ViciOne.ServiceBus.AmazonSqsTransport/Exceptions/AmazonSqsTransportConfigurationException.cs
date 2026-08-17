namespace ViciOne.ServiceBus;

using System;
using System.Runtime.Serialization;


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

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected AmazonSqsTransportConfigurationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
