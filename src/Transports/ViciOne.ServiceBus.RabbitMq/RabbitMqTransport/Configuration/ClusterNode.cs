using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Identifies a RabbitMQ cluster node by host name and optional TCP port.</summary>
public readonly record struct ClusterNode :
    IParsable<ClusterNode>,
    ISpanParsable<ClusterNode>
{
    /// <summary>Gets the stored host name or IP address.</summary>
    public string HostName { get; }

    /// <summary>Gets the explicit AMQP port, or <see langword="null"/> to use the connection-factory default.</summary>
    public int? Port { get; }

    private ClusterNode(string hostName, int? port)
    {
        HostName = hostName;
        Port = port;
    }

    /// <summary>Returns the stored host and optional port representation, adding brackets around IPv6 hosts.</summary>
    /// <returns>The stored host and optional port, with brackets around IPv6 hosts.</returns>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(HostName))
            return string.Empty;

        string host = HostName.Contains(':', StringComparison.Ordinal)
            ? $"[{HostName}]"
            : HostName;
        return Port is { } port
            ? string.Concat(host, ":", port.ToString(CultureInfo.InvariantCulture))
            : host;
    }

    /// <summary>Parses a RabbitMQ cluster-node representation.</summary>
    /// <param name="address">A DNS name, IPv4 address, or bracketed/unbracketed IPv6 address with an optional port.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="address" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="address" /> is not a valid node.</exception>
    public static ClusterNode Parse(string address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return Parse(address.AsSpan(), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public static ClusterNode Parse(string address, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(address);
        return Parse(address.AsSpan(), provider);
    }

    /// <summary>Parses a RabbitMQ cluster-node representation from a character span.</summary>
    /// <param name="address">The node representation to parse.</param>
    /// <param name="provider">Ignored because node syntax is culture-independent.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentException"><paramref name="address" /> is not a valid node.</exception>
    public static ClusterNode Parse(ReadOnlySpan<char> address, IFormatProvider? provider = null)
    {
        if (TryParse(address, provider, out ClusterNode result))
            return result;

        throw new ArgumentException($"Invalid RabbitMQ cluster node: '{address.ToString()}'.", nameof(address));
    }

    /// <summary>Attempts to parse a RabbitMQ cluster-node representation.</summary>
    /// <param name="address">The node representation to parse.</param>
    /// <param name="result">The parsed node when successful.</param>
    /// <returns><see langword="true" /> when parsing succeeds; otherwise, <see langword="false" />.</returns>
    public static bool TryParse([NotNullWhen(true)] string? address, out ClusterNode result) =>
        TryParse(address, CultureInfo.InvariantCulture, out result);

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? address, IFormatProvider? provider, out ClusterNode result)
    {
        if (address is null)
        {
            result = default;
            return false;
        }

        return TryParse(address.AsSpan(), provider, out result);
    }

    /// <inheritdoc />
    public static bool TryParse(ReadOnlySpan<char> address, IFormatProvider? provider, out ClusterNode result)
    {
        result = default;
        if (address.IsEmpty || ContainsWhitespace(address))
            return false;

        ReadOnlySpan<char> host;
        int? port = null;
        if (address[0] == '[')
        {
            int closeBracket = address.IndexOf(']');
            if (closeBracket <= 1)
                return false;

            host = address[1..closeBracket];
            if (!IsIpv6(host))
                return false;

            ReadOnlySpan<char> suffix = address[(closeBracket + 1)..];
            if (!suffix.IsEmpty)
            {
                if (suffix[0] != ':' || !TryParsePort(suffix[1..], out int parsedPort))
                    return false;

                port = parsedPort;
            }
        }
        else
        {
            int firstColon = address.IndexOf(':');
            if (firstColon < 0)
                host = address;
            else if (firstColon == address.LastIndexOf(':'))
            {
                host = address[..firstColon];
                if (!TryParsePort(address[(firstColon + 1)..], out int parsedPort))
                    return false;

                port = parsedPort;
            }
            else
            {
                host = address;
                if (!IsIpv6(host))
                    return false;
            }
        }

        if (!IsValidHost(host))
            return false;

        result = new ClusterNode(host.ToString(), port);
        return true;
    }

    private static bool IsValidHost(ReadOnlySpan<char> host)
    {
        if (host.IsEmpty || host.Contains('[') || host.Contains(']'))
            return false;

        if (host.Contains(':'))
            return IsIpv6(host);

        return Uri.CheckHostName(host.ToString()) != UriHostNameType.Unknown;
    }

    private static bool IsIpv6(ReadOnlySpan<char> host) =>
        IPAddress.TryParse(host, out IPAddress? address) && address.AddressFamily == AddressFamily.InterNetworkV6;

    private static bool TryParsePort(ReadOnlySpan<char> value, out int port) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out port) && port is >= 1 and <= 65535;

    private static bool ContainsWhitespace(ReadOnlySpan<char> value)
    {
        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character))
                return true;
        }

        return false;
    }
}
