using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq message name formatter implementation.
/// </summary>
public class RabbitMqMessageNameFormatter :
    IMessageNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RabbitMqMessageNameFormatter()
        : this(true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="includeNamespace">The include namespace value.</param>
    public RabbitMqMessageNameFormatter(bool includeNamespace)
    {
        _formatter = new DefaultMessageNameFormatter("::", "--", ":", "-", includeNamespace);
    }

    /// <summary>
    /// Gets message name.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetMessageName(Type type)
    {
        return _formatter.GetMessageName(type);
    }
}
