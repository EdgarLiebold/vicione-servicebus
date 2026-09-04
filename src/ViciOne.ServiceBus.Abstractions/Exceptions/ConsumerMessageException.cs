using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class ConsumerMessageException :
    ConsumerException
{
    public ConsumerMessageException()
    {
    }

    public ConsumerMessageException(string message)
        : base(message)
    {
    }

    public ConsumerMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
