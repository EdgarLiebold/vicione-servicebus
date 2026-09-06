using System;
using System.Diagnostics;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Parses and formats an Azure Service Bus namespace address and optional scope.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct ServiceBusHostAddress
{
    /// <summary>The Azure Service Bus URI scheme.</summary>
    public readonly string Scheme;
    /// <summary>The Azure Service Bus namespace host.</summary>
    public readonly string Host;
    /// <summary>The optional namespace-relative entity-path scope.</summary>
    public readonly string Scope;

    /// <summary>Parses an absolute Azure Service Bus namespace address.</summary>
    /// <param name="address">The namespace address to parse.</param>
    public ServiceBusHostAddress(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        Scheme = null!;
        Host = null!;
        Scope = null!;

        var scheme = address.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case "sb":
                Scheme = address.Scheme;
                Host = address.Host;

                ParseLeft(address, out Scheme, out Host, out Scope);
                break;

            default:
                throw new ArgumentException($"The address scheme is not supported: {address.Scheme}", nameof(address));
        }
    }

    static void ParseLeft(Uri address, out string scheme, out string host, out string scope)
    {
        scheme = address.Scheme;
        host = address.Host;

        scope = address.ParseHostPath();
    }

    /// <summary>Formats the namespace and optional scope as an absolute URI.</summary>
    /// <param name="address">The host address to format.</param>
    /// <returns>The absolute Azure Service Bus namespace URI.</returns>
    public static implicit operator Uri(in ServiceBusHostAddress address)
    {
        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host,
            Path = address.Scope == "/"
                ? "/"
                : $"/{Uri.EscapeDataString(address.Scope)}"
        };

        return builder.Uri;
    }

    Uri DebuggerDisplay => this;
}
