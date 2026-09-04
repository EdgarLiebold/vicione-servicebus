using System;

namespace ViciOne.ServiceBus;

public sealed class RabbitMqAddressException :
    ConfigurationException
{
    public RabbitMqAddressException()
        : base("The RabbitMQ address is invalid.")
    {
    }

    public RabbitMqAddressException(string message)
        : base(message)
    {
    }

    public RabbitMqAddressException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
