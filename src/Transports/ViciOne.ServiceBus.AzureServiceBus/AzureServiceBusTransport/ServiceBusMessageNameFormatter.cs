using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus message name formatter implementation.
/// </summary>
public class ServiceBusMessageNameFormatter :
    IMessageNameFormatter
{
    readonly IMessageNameFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="namespaceSeparator">The namespace separator value.</param>
    public ServiceBusMessageNameFormatter(string? namespaceSeparator = null)
        : this(true, namespaceSeparator)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="includeNamespace">The include namespace value.</param>
    /// <param name="namespaceSeparator">The namespace separator value.</param>
    public ServiceBusMessageNameFormatter(bool includeNamespace, string? namespaceSeparator = null)
    {
        _formatter = string.IsNullOrWhiteSpace(namespaceSeparator)
            ? new DefaultMessageNameFormatter("---", "--", "/", "-", includeNamespace)
            : new DefaultMessageNameFormatter("---", "--", namespaceSeparator, "-", includeNamespace);
    }

    /// <summary>
    /// Gets message name.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetMessageName(Type type)
    {
        return _formatter.GetMessageName(type).Replace("[]", "__");
    }
}
