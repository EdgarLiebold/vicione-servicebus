using System;
using System.Diagnostics;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Represents an in memory host address.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct InMemoryHostAddress
{
    const string InMemorySchema = "loopback";

    /// <summary>Exposes the scheme used by the containing type.</summary>
    public readonly string Scheme;
    /// <summary>Exposes the host used by the containing type.</summary>
    public readonly string Host;
    /// <summary>Exposes the virtual host used by the containing type.</summary>
    public readonly string VirtualHost;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="address">The address.</param>
    public InMemoryHostAddress(Uri address)
    {
        Scheme = null!;
        Host = null!;
        VirtualHost = null!;

        var scheme = address.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case InMemorySchema:
                ParseLeft(address, out Scheme, out Host, out VirtualHost);
                break;

            default:
                throw new ArgumentException($"The address scheme is not supported: {address.Scheme}", nameof(address));
        }
    }

    static void ParseLeft(Uri address, out string scheme, out string host, out string virtualHost)
    {
        scheme = address.Scheme;
        host = address.Host;
        virtualHost = address.ParseHostPath();
    }

    /// <summary>Converts a value to <see cref="Uri" />.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The value produced by the operation.</returns>
    public static implicit operator Uri(in InMemoryHostAddress address)
    {
        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host,
            Path = address.VirtualHost == "/"
                ? "/"
                : $"/{Uri.EscapeDataString(address.VirtualHost)}"
        };

        return builder.Uri;
    }

    Uri DebuggerDisplay => this;
}
