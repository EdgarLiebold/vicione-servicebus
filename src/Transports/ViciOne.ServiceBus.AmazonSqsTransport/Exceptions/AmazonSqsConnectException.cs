using System;

namespace ViciOne.ServiceBus;

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
}
