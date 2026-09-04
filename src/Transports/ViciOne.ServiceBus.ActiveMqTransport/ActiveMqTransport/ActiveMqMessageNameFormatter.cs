using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ActiveMqMessageNameFormatter :
    IMessageNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    public ActiveMqMessageNameFormatter()
        : this(true)
    {
    }

    public ActiveMqMessageNameFormatter(bool includeNamespace)
    {
        _formatter = new DefaultMessageNameFormatter("::", "--", ".", "-", includeNamespace);
    }

    public string GetMessageName(Type type)
    {
        return _formatter.GetMessageName(type);
    }
}
