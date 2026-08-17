namespace ViciOne.ServiceBus;

using System;
using System.Runtime.Serialization;


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

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected AmazonSqsTransportException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
