using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class ActiveMqConnectionException :
    ConnectionException
{
    public ActiveMqConnectionException()
    {
    }

    public ActiveMqConnectionException(string message)
        : base(message)
    {
    }

    public ActiveMqConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
