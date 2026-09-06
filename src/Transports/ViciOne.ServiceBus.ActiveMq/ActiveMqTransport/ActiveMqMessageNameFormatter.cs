using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Formats .NET message types as ActiveMQ entity-name segments.</summary>
public class ActiveMqMessageNameFormatter :
    IMessageNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    /// <summary>Creates a formatter that includes namespaces in message names.</summary>
    public ActiveMqMessageNameFormatter()
        : this(true)
    {
    }

    /// <summary>Creates a formatter with configurable namespace inclusion.</summary>
    /// <param name="includeNamespace">Whether to include the declaring namespace in formatted names.</param>
    public ActiveMqMessageNameFormatter(bool includeNamespace)
    {
        _formatter = new DefaultMessageNameFormatter("::", "--", ".", "-", includeNamespace);
    }

    /// <summary>Formats a message type as an ActiveMQ-compatible name.</summary>
    /// <param name="type">The message type to format.</param>
    /// <returns>The formatted message name.</returns>
    public string GetMessageName(Type type)
    {
        return _formatter.GetMessageName(type);
    }
}
