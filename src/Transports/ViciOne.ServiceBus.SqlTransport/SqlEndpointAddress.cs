using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Identifies a SQL transport queue or topic within a logical host.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct SqlEndpointAddress
{
    const string InstanceNameKey = "instance";
    const string AutoDeleteKey = "autodelete";
    const string KindKey = "kind";

    /// <summary>Gets the SQL transport URI scheme.</summary>
    public string Scheme { get; }
    /// <summary>Gets the database server host.</summary>
    public string Host { get; }
    /// <summary>Gets the optional SQL Server instance name.</summary>
    public string? InstanceName { get; }
    /// <summary>Gets the optional database server port.</summary>
    public int? Port { get; }
    /// <summary>Gets the logical transport namespace.</summary>
    public string VirtualHost { get; }
    /// <summary>Gets the optional queue area.</summary>
    public string? Area { get; }
    /// <summary>Gets the queue or topic name.</summary>
    public string Name { get; }
    /// <summary>Gets the optional queue idle period after which the queue is deleted.</summary>
    public TimeSpan? AutoDeleteOnIdle { get; }
    /// <summary>Gets the destination kind.</summary>
    public SqlEndpointKind Kind { get; }

    /// <summary>Resolves a queue, topic, or absolute SQL endpoint address against a host.</summary>
    /// <param name="hostAddress">The absolute SQL transport host address.</param>
    /// <param name="address">The endpoint address to resolve.</param>
    /// <param name="kind">The destination kind used when the address does not specify one.</param>
    public SqlEndpointAddress(Uri hostAddress, Uri address, SqlEndpointKind kind = SqlEndpointKind.Queue)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentNullException.ThrowIfNull(address);
        ValidateKind(kind, nameof(kind));

        Port = default;

        AutoDeleteOnIdle = null;
        Kind = kind;

        ParseLeft(hostAddress, out string parsedScheme, out string parsedHost, out string? parsedInstanceName,
            out int? parsedPort, out string parsedVirtualHost, out string? parsedArea);
        Scheme = parsedScheme;
        Host = parsedHost;
        InstanceName = parsedInstanceName;
        Port = parsedPort;
        VirtualHost = parsedVirtualHost;
        Area = parsedArea;

        var scheme = address.Scheme.ToLowerInvariant();
        string endpointName;
        switch (scheme)
        {
            case SqlHostAddress.SchemeName:

                address.ParseHostPathAndEntityName(out _, out endpointName!);

                if (string.IsNullOrWhiteSpace(endpointName))
                    throw new SqlEndpointAddressException(address, "Endpoint name must be specified");
                break;

            case "queue":
                endpointName = address.AbsolutePath;
                break;

            case "topic":
                Area = default;
                endpointName = address.AbsolutePath;
                Kind = SqlEndpointKind.Topic;
                break;

            default:
                throw new SqlEndpointAddressException(address, "Scheme is not supported");
        }

        Name = endpointName;

        foreach (var (key, value) in address.SplitQueryString())
        {
            switch (key)
            {
                case AutoDeleteKey:
                    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
                        || !double.IsFinite(seconds)
                        || seconds <= 0
                        || seconds > int.MaxValue)
                    {
                        throw new SqlEndpointAddressException(address, "The auto-delete interval must be a positive number of seconds.");
                    }

                    AutoDeleteOnIdle = TimeSpan.FromSeconds(seconds);
                    if (AutoDeleteOnIdle <= TimeSpan.Zero)
                        throw new SqlEndpointAddressException(address, "The auto-delete interval must be a positive number of seconds.");
                    break;

                case KindKey:
                    if (!Enum.TryParse(value, true, out SqlEndpointKind parsedKind) || !Enum.IsDefined(parsedKind))
                        throw new SqlEndpointAddressException(address, "The endpoint kind must be either queue or topic.");

                    Kind = parsedKind;
                    break;
            }
        }

        ValidateName(Name, address);

        if (Kind == SqlEndpointKind.Topic && AutoDeleteOnIdle.HasValue)
            throw new SqlEndpointAddressException(address, "Topics do not support an auto-delete interval.");
    }

    /// <summary>Creates a queue or topic address from validated components.</summary>
    /// <param name="hostAddress">The absolute SQL transport host address.</param>
    /// <param name="name">The queue or topic name.</param>
    /// <param name="autoDeleteOnIdle">The optional queue idle period after which the queue is deleted.</param>
    /// <param name="kind">The destination kind.</param>
    public SqlEndpointAddress(Uri hostAddress, string name, TimeSpan? autoDeleteOnIdle = null, SqlEndpointKind kind = SqlEndpointKind.Queue)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateKind(kind, nameof(kind));
        if (autoDeleteOnIdle <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(autoDeleteOnIdle), autoDeleteOnIdle, "The auto-delete interval must be greater than zero.");
        if (autoDeleteOnIdle > TimeSpan.FromSeconds(int.MaxValue))
            throw new ArgumentOutOfRangeException(nameof(autoDeleteOnIdle), autoDeleteOnIdle, "The auto-delete interval exceeds the SQL seconds limit.");
        if (kind == SqlEndpointKind.Topic && autoDeleteOnIdle.HasValue)
            throw new ArgumentException("Topics do not support an auto-delete interval.", nameof(autoDeleteOnIdle));

        ParseLeft(hostAddress, out string parsedScheme, out string parsedHost, out string? parsedInstanceName,
            out int? parsedPort, out string parsedVirtualHost, out string? parsedArea);
        Scheme = parsedScheme;
        Host = parsedHost;
        InstanceName = parsedInstanceName;
        Port = parsedPort;
        VirtualHost = parsedVirtualHost;
        Area = parsedArea;

        Name = name;
        ValidateName(Name, hostAddress);

        AutoDeleteOnIdle = autoDeleteOnIdle;

        Kind = kind;

        if (kind == SqlEndpointKind.Topic)
            Area = default;
    }

    static void ParseLeft(Uri address, out string scheme, out string host, out string? instanceName, out int? port, out string virtualHost,
        out string? area)
    {
        var hostAddress = new SqlHostAddress(address);
        scheme = hostAddress.Scheme;
        host = hostAddress.Host;
        instanceName = hostAddress.InstanceName;
        port = address.IsDefaultPort ? null : address.Port;
        virtualHost = hostAddress.VirtualHost;
        area = hostAddress.Area;
    }

    /// <summary>Formats a validated SQL endpoint address as an absolute URI.</summary>
    /// <param name="address">The SQL endpoint address.</param>
    /// <returns>The absolute SQL transport URI.</returns>
    public static implicit operator Uri(in SqlEndpointAddress address)
    {
        if (string.IsNullOrWhiteSpace(address.Scheme)
            || string.IsNullOrWhiteSpace(address.Host)
            || string.IsNullOrWhiteSpace(address.VirtualHost)
            || string.IsNullOrWhiteSpace(address.Name))
        {
            throw new InvalidOperationException("The SQL endpoint address has not been initialized.");
        }

        var path = address.VirtualHost == "/" ? "/" : Uri.EscapeDataString(address.VirtualHost);
        if (!string.IsNullOrWhiteSpace(address.Area) && address.Kind == SqlEndpointKind.Queue)
            path += "." + Uri.EscapeDataString(address.Area);
        if (path[path.Length - 1] != '/')
            path += '/';
        path += address.Name;

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
        if (AutoDeleteOnIdle.HasValue && Kind == SqlEndpointKind.Queue)
            yield return $"{AutoDeleteKey}={AutoDeleteOnIdle.Value.TotalSeconds.ToString("R", CultureInfo.InvariantCulture)}";

        if (Kind != SqlEndpointKind.Queue)
            yield return $"{KindKey}=topic";

        if (!string.IsNullOrEmpty(InstanceName))
            yield return $"{InstanceNameKey}={Uri.EscapeDataString(InstanceName)}";
    }

    static void ValidateKind(SqlEndpointKind kind, string parameterName)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(parameterName, kind, "The endpoint kind must be either queue or topic.");
    }

    static void ValidateName(string name, Uri address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new SqlEndpointAddressException(address, "An endpoint name is required.");

        foreach (char character in name)
        {
            if (!(char.IsLetterOrDigit(character) || character is '-' or '_' or '.' or ':'))
            {
                throw new SqlEndpointAddressException(address,
                    "The endpoint name may contain only letters, digits, hyphens, underscores, periods, and colons.");
            }
        }
    }
}
