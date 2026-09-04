#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Represents a sql endpoint address value.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct SqlEndpointAddress
{
    const string InstanceNameKey = "instance";
    const string AutoDeleteKey = "autodelete";
    const string TypeKey = "type";


    /// <summary>
    /// Specifies the available address type values.
    /// </summary>
    public enum AddressType
    {
        /// <summary>
        /// Indicates queue.
        /// </summary>
        Queue = 0,
        /// <summary>
        /// Indicates topic.
        /// </summary>
        Topic = 1
    }


    static readonly ITypeConverter<AddressType, string> _parseConverter = new EnumTypeConverter<AddressType>();

    /// <summary>
    /// Defines the scheme value.
    /// </summary>
    public readonly string Scheme;
    /// <summary>
    /// Defines the host value.
    /// </summary>
    public readonly string Host;
    /// <summary>
    /// Defines the instance name value.
    /// </summary>
    public readonly string? InstanceName;
    /// <summary>
    /// Defines the port value.
    /// </summary>
    public readonly int? Port;
    /// <summary>
    /// Defines the virtual host value.
    /// </summary>
    public readonly string VirtualHost;
    /// <summary>
    /// Defines the area value.
    /// </summary>
    public readonly string? Area;
    /// <summary>
    /// Defines the name value.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Defines the auto delete on idle value.
    /// </summary>
    public readonly TimeSpan? AutoDeleteOnIdle;
    /// <summary>
    /// Defines the type value.
    /// </summary>
    public readonly AddressType Type;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="type">The type value.</param>
    public SqlEndpointAddress(Uri hostAddress, Uri address, AddressType type = AddressType.Queue)
    {
        Port = default;

        AutoDeleteOnIdle = null;
        Type = type;

        ParseLeft(hostAddress, out Scheme, out Host, out InstanceName, out Port, out VirtualHost, out Area);

        var scheme = address.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case SqlHostAddress.DbScheme:

                address.ParseHostPathAndEntityName(out _, out Name!);

                if (string.IsNullOrWhiteSpace(Name))
                    throw new SqlEndpointAddressException(address, "Endpoint name must be specified");
                break;

            case "queue":
                Name = address.AbsolutePath;
                break;

            case "topic":
                Area = default;
                Name = address.AbsolutePath;
                Type = AddressType.Topic;
                break;

            default:
                throw new SqlEndpointAddressException(address, "Scheme is not supported");
        }

        foreach (var (key, value) in address.SplitQueryString())
        {
            switch (key)
            {
                case AutoDeleteKey when int.TryParse(value, out var result):
                    AutoDeleteOnIdle = TimeSpan.FromSeconds(result);
                    break;

                case TypeKey when value != null && _parseConverter.TryConvert(value, out var result):
                    Type = result;
                    break;
            }
        }

        if (Type != AddressType.Queue && AutoDeleteOnIdle.HasValue)
            AutoDeleteOnIdle = null;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle value.</param>
    /// <param name="type">The type value.</param>
    public SqlEndpointAddress(Uri hostAddress, string name, TimeSpan? autoDeleteOnIdle = null, AddressType type = AddressType.Queue)
    {
        ParseLeft(hostAddress, out Scheme, out Host, out InstanceName, out Port, out VirtualHost, out Area);

        Name = name;

        AutoDeleteOnIdle = type == AddressType.Queue ? autoDeleteOnIdle : null;

        Type = type;

        if (type == AddressType.Topic)
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

    /// <summary>
    /// Converts a value to <see cref="Uri" />.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator Uri(in SqlEndpointAddress address)
    {
        var path = address.VirtualHost == "/" ? "/" : Uri.EscapeDataString(address.VirtualHost);
        if (!string.IsNullOrWhiteSpace(address.Area) && address.Type == AddressType.Queue)
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

        builder.Query += string.Join("&", address.GetQueryStringOptions());

        return builder.Uri;
    }

    Uri DebuggerDisplay => this;

    IEnumerable<string> GetQueryStringOptions()
    {
        if (AutoDeleteOnIdle.HasValue && Type == AddressType.Queue)
            yield return $"{AutoDeleteKey}={AutoDeleteOnIdle.Value.TotalSeconds:F0}";

        if (Type != AddressType.Queue)
            yield return $"{TypeKey}=topic";

        if (!string.IsNullOrEmpty(InstanceName))
            yield return $"{InstanceNameKey}={InstanceName}";
    }
}
