using System;
using System.Diagnostics;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Represents a service bus host address value.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct ServiceBusHostAddress
{
    /// <summary>
    /// Defines the scheme value.
    /// </summary>
    public readonly string Scheme;
    /// <summary>
    /// Defines the host value.
    /// </summary>
    public readonly string Host;
    /// <summary>
    /// Defines the scope value.
    /// </summary>
    public readonly string Scope;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
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

    /// <summary>
    /// Converts a value to <see cref="Uri" />.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
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
