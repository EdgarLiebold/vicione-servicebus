using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Identifies one logical SQL transport host using the form
/// <c>db://server/virtual_host.area</c>.
/// <list type="table">
/// <listheader>
/// <term>Fragment</term>
/// <description>Description</description>
/// </listheader>
/// <item>
/// <term>Host</term>
/// <description>The host name from the connection string, or the host alias if configured</description>
/// </item>
/// <item>
/// <term>Virtual Host</term>
/// <description>
/// The name for an isolated set of topics, queues, and subscriptions in the host/schema specified by the connection string.
/// If not specified, the default virtual host is used.
/// </description>
/// </item>
/// <item>
/// <term>Area</term>
/// <description>The name an area, which contains one or more queues. If not specified, the default area within the virtual host is used.</description>
/// </item>
/// </list>
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct SqlHostAddress
{
    const string InstanceNameKey = "instance";
    internal const string SchemeName = "db";

    /// <summary>Gets the SQL transport URI scheme.</summary>
    public string Scheme { get; }
    /// <summary>Gets the database server host.</summary>
    public string Host { get; }
    /// <summary>Gets the optional database server port.</summary>
    public int? Port { get; }
    /// <summary>Gets the optional SQL Server instance name.</summary>
    public string? InstanceName { get; }
    /// <summary>Gets the logical transport namespace hosted by the database.</summary>
    public string VirtualHost { get; }
    /// <summary>Gets the optional queue area within the virtual host.</summary>
    public string? Area { get; }

    /// <summary>Parses an absolute SQL transport host address.</summary>
    /// <param name="address">The absolute host address to parse.</param>
    public SqlHostAddress(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
            throw new ArgumentException("The SQL host address must be absolute.", nameof(address));

        var scheme = address.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case SchemeName:
                ParseLeft(address, out string parsedScheme, out string parsedHost, out int? parsedPort,
                    out string parsedVirtualHost, out string? parsedArea);
                Scheme = parsedScheme;
                Host = parsedHost;
                Port = parsedPort;
                VirtualHost = parsedVirtualHost;
                Area = parsedArea;
                break;

            default:
                throw new ArgumentException($"The address scheme is not supported: {address.Scheme}", nameof(address));
        }

        foreach (var (key, value) in address.SplitQueryString())
        {
            switch (key)
            {
                case InstanceNameKey when !string.IsNullOrWhiteSpace(value):
                    InstanceName = Uri.UnescapeDataString(value);
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(Host))
            throw new SqlEndpointAddressException(address, "A database server host is required.");
    }

    /// <summary>Creates a SQL transport host address from validated components.</summary>
    /// <param name="host">The database server host.</param>
    /// <param name="instanceName">The optional SQL Server instance name.</param>
    /// <param name="port">The optional database server port.</param>
    /// <param name="virtualHost">The logical transport namespace.</param>
    /// <param name="area">The optional queue area.</param>
    public SqlHostAddress(string host, string? instanceName, int? port, string virtualHost, string? area)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(virtualHost);
        if (!CanRepresentNetworkHost(host))
            throw new ArgumentException("The SQL host must identify a network host; Unix socket paths are not supported.", nameof(host));
        if (port is <= 0 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), port, "The SQL host port must be between 1 and 65535.");
        if (instanceName != null && string.IsNullOrWhiteSpace(instanceName))
            throw new ArgumentException("The SQL Server instance name cannot be empty or whitespace.", nameof(instanceName));
        if (virtualHost != "/" && !IsValidSymbol(virtualHost))
            throw new ArgumentException("The virtual host must start with a letter or underscore and contain only letters, digits, or underscores.", nameof(virtualHost));
        if (virtualHost == "/" && area != null)
            throw new ArgumentException("An area requires a named virtual host.", nameof(area));
        if (area != null && !IsValidSymbol(area))
            throw new ArgumentException("The area must start with a letter or underscore and contain only letters, digits, or underscores.", nameof(area));

        Scheme = SchemeName;
        Host = host.Trim();
        InstanceName = instanceName;
        Port = port;
        VirtualHost = virtualHost;
        Area = area;
    }

    internal static void ParseLeft(Uri address, out string scheme, out string host, out int? port, out string virtualHost, out string? area)
    {
        scheme = address.Scheme;
        host = address.Host;
        port = address.IsDefaultPort ? null : address.Port;

        (virtualHost, area) = GetVirtualHostAndArea(address);
    }

    static (string virtualHost, string? area) GetVirtualHostAndArea(Uri address)
    {
        var path = address.AbsolutePath;

        if (string.IsNullOrWhiteSpace(path))
            return ("/", null);

        if (path.Length == 1 && path[0] == '/')
            return ("/", null);

        var split = path.LastIndexOf('/');

        ReadOnlySpan<char> span = split > 0
            ? path.AsSpan(1, split - 1)
            : path.AsSpan(1);

        string virtualHost;
        string? area = null;

        var areaSplit = span.IndexOf('.');
        if (areaSplit > 0)
        {
            virtualHost = Uri.UnescapeDataString(span.Slice(0, areaSplit).ToString()).Trim();
            area = Uri.UnescapeDataString(span.Slice(areaSplit + 1).ToString()).Trim();
        }
        else
            virtualHost = Uri.UnescapeDataString(span.ToString()).Trim();

        if (string.IsNullOrWhiteSpace(virtualHost))
            return ("/", null);

        if (!IsValidSymbol(virtualHost))
            throw new SqlEndpointAddressException(address,
                "The virtual host must start with a letter or underscore and contain only letters, digits, or underscores.");

        if (string.IsNullOrWhiteSpace(area))
            return (virtualHost, null);

        if (!IsValidSymbol(area))
            throw new SqlEndpointAddressException(address,
                "The area must start with a letter or underscore and contain only letters, digits, or underscores.");

        return (virtualHost, area);
    }

    /// <summary>Formats a validated SQL host address as an absolute URI.</summary>
    /// <param name="address">The SQL host address.</param>
    /// <returns>The absolute SQL transport URI.</returns>
    public static implicit operator Uri(in SqlHostAddress address)
    {
        if (string.IsNullOrWhiteSpace(address.Scheme)
            || string.IsNullOrWhiteSpace(address.Host)
            || string.IsNullOrWhiteSpace(address.VirtualHost))
        {
            throw new InvalidOperationException("The SQL host address has not been initialized.");
        }

        var path = address.VirtualHost == "/" ? "/" : Uri.EscapeDataString(address.VirtualHost);
        if (!string.IsNullOrWhiteSpace(address.Area))
            path += "." + Uri.EscapeDataString(address.Area);

        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host.Trim().Trim('(', ')'),
            Port = address.Port ?? -1,
            Path = path
        };


        builder.Query = string.Join("&", address.GetQueryStringOptions());

        return builder.Uri;
    }

    Uri DebuggerDisplay => this;

    IEnumerable<string> GetQueryStringOptions()
    {
        if (!string.IsNullOrEmpty(InstanceName))
            yield return $"{InstanceNameKey}={Uri.EscapeDataString(InstanceName)}";
    }

    internal static bool IsValidSymbol(string? className)
    {
        if (string.IsNullOrEmpty(className))
            return false;

        var c0 = className![0];
        if (!(char.IsLetter(c0) || c0 == '_'))
            return false;

        for (var i = 1; i < className.Length; i++)
        {
            var c = className[i];
            if (!(char.IsLetterOrDigit(c) || c == '_'))
                return false;
        }

        return true;
    }

    internal static bool CanRepresentNetworkHost(string host)
    {
        char first = host.TrimStart()[0];
        return first is not '/' and not '@';
    }
}
