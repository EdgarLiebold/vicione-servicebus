using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class ConsumerCanceledException :
    ViciOneServiceBusException
{
    public ConsumerCanceledException()
    {
    }

    public ConsumerCanceledException(string message)
        : base(message)
    {
    }

    public ConsumerCanceledException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
