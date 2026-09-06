using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Formats .NET message types as Azure Service Bus entity-name components.</summary>
public class ServiceBusMessageNameFormatter :
    IMessageNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    /// <summary>Creates a formatter that includes namespaces.</summary>
    /// <param name="namespaceSeparator">The optional separator used between namespace components.</param>
    public ServiceBusMessageNameFormatter(string? namespaceSeparator = null)
        : this(true, namespaceSeparator)
    {
    }

    /// <summary>Creates a formatter with configurable namespace inclusion.</summary>
    /// <param name="includeNamespace">Whether the formatted entity name includes the contract namespace.</param>
    /// <param name="namespaceSeparator">The optional separator used between namespace components.</param>
    public ServiceBusMessageNameFormatter(bool includeNamespace, string? namespaceSeparator = null)
    {
        _formatter = string.IsNullOrWhiteSpace(namespaceSeparator)
            ? new DefaultMessageNameFormatter("---", "--", "/", "-", includeNamespace)
            : new DefaultMessageNameFormatter("---", "--", namespaceSeparator, "-", includeNamespace);
    }

    /// <summary>Formats a message contract type and replaces array suffixes with Azure-safe characters.</summary>
    /// <param name="type">The message contract type.</param>
    /// <returns>The Azure-compatible entity-name component.</returns>
    public string GetMessageName(Type type)
    {
        return _formatter.GetMessageName(type).Replace("[]", "__");
    }
}
