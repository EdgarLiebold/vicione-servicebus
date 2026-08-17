namespace ViciOne.ServiceBus;

using System;


[Serializable]
public class AmazonSqsConnectionException :
    ConnectionException
{
    public AmazonSqsConnectionException()
    {
    }

    public AmazonSqsConnectionException(string message)
        : base(message)
    {
    }

    public AmazonSqsConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
