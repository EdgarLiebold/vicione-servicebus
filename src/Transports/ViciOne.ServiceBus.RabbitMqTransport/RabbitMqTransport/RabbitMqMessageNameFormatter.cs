using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public class RabbitMqMessageNameFormatter :
    IMessageNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    public RabbitMqMessageNameFormatter()
        : this(true)
    {
    }

    public RabbitMqMessageNameFormatter(bool includeNamespace)
    {
        _formatter = new DefaultMessageNameFormatter("::", "--", ":", "-", includeNamespace);
    }

    public string GetMessageName(Type type)
    {
        return _formatter.GetMessageName(type);
    }
}
