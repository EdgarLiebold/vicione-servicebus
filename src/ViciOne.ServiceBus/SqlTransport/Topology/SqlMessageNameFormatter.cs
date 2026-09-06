using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Formats sql message name values.</summary>
public class SqlMessageNameFormatter :
    IMessageNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="namespaceSeparator">The namespace separator.</param>
    public SqlMessageNameFormatter(string? namespaceSeparator = null)
        : this(true, namespaceSeparator)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="includeNamespace">The include namespace.</param>
    /// <param name="namespaceSeparator">The namespace separator.</param>
    public SqlMessageNameFormatter(bool includeNamespace, string? namespaceSeparator = null)
    {
        _formatter = string.IsNullOrWhiteSpace(namespaceSeparator)
            ? new DefaultMessageNameFormatter("::", "--", ":", "-", includeNamespace)
            : new DefaultMessageNameFormatter("::", "--", namespaceSeparator, "-", includeNamespace);
    }

    /// <summary>Gets message name.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <returns>The message name.</returns>
    public string GetMessageName(Type type)
    {
        return _formatter.GetMessageName(type);
    }
}
