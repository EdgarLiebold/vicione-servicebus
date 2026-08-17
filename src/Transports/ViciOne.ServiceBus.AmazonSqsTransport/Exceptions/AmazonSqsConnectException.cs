namespace ViciOne.ServiceBus;

using System;
using System.Runtime.Serialization;


[Serializable]
public class AmazonSqsConnectException :
    AmazonSqsConnectionException
{
    public AmazonSqsConnectException()
    {
    }

    public AmazonSqsConnectException(string message)
        : base(message)
    {
    }

    public AmazonSqsConnectException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected AmazonSqsConnectException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
