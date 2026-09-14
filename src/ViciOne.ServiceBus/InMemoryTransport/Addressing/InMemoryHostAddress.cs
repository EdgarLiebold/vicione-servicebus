using System;
using System.Diagnostics;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.InMemoryTransport.Addressing;

/// <summary>Parses and normalizes an in-memory transport host address.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
internal readonly struct InMemoryHostAddress
{
    internal const string InMemoryScheme = "loopback";

    /// <summary>Parses a loopback host address.</summary>
    /// <param name="address">The absolute host address to parse.</param>
    public InMemoryHostAddress(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
            throw new ArgumentException("The in-memory host address must be absolute.", nameof(address));
        if (!string.IsNullOrEmpty(address.UserInfo) || !string.IsNullOrEmpty(address.Query) || !string.IsNullOrEmpty(address.Fragment))
        {
            throw new ArgumentException(
                "Credentials, query options, and fragments are not valid in-memory host address components.",
                nameof(address));
        }
        if (!address.IsDefaultPort)
            throw new ArgumentException("The in-memory transport does not support explicit ports.", nameof(address));

        if (!string.Equals(address.Scheme, InMemoryScheme, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"The in-memory address scheme is not supported: {address.Scheme}", nameof(address));

        string escapedPath = address.AbsolutePath.Trim('/');
        if (escapedPath.Contains('/', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "An in-memory virtual host must be encoded as one URI path component.",
                nameof(address));
        }

        Scheme = InMemoryScheme;
        Host = address.Host;
        VirtualHost = escapedPath.Length == 0 ? "/" : Uri.UnescapeDataString(escapedPath);

        if (string.IsNullOrWhiteSpace(Host))
            throw new ArgumentException("The in-memory host address must include a host name.", nameof(address));
    }

    /// <summary>Gets the normalized <c>loopback</c> scheme.</summary>
    public string Scheme { get; }

    /// <summary>Gets the logical in-memory host name.</summary>
    public string Host { get; }

    /// <summary>Gets the decoded virtual-host identity.</summary>
    public string VirtualHost { get; }

    /// <summary>Converts the normalized host address to its absolute URI.</summary>
    /// <param name="address">The normalized in-memory host address.</param>
    /// <returns>The canonical loopback URI.</returns>
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
