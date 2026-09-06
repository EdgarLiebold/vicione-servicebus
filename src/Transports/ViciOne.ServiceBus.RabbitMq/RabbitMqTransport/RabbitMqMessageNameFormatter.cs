using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Formats message-contract types as RabbitMQ-safe exchange names.</summary>
public class RabbitMqMessageNameFormatter :
    IMessageNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    /// <summary>Creates a formatter that includes the contract namespace.</summary>
    public RabbitMqMessageNameFormatter()
        : this(true)
    {
    }

    /// <summary>Creates a formatter with explicit namespace inclusion.</summary>
    /// <param name="includeNamespace">Whether the contract namespace participates in the exchange name.</param>
    public RabbitMqMessageNameFormatter(bool includeNamespace)
    {
        _formatter = new DefaultMessageNameFormatter("::", "--", ":", "-", includeNamespace);
    }

    /// <summary>Formats a contract type using RabbitMQ namespace and nested-type separators.</summary>
    /// <param name="type">The message-contract type.</param>
    /// <returns>The RabbitMQ exchange name.</returns>
    public string GetMessageName(Type type)
    {
        return _formatter.GetMessageName(type);
    }
}
