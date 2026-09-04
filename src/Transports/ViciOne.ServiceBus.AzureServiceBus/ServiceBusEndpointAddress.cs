using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Represents a service bus endpoint address value.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct ServiceBusEndpointAddress
{
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
    /// Defines the scope value.
    /// </summary>
    public readonly string Scope;

    /// <summary>
    /// Defines the name value.
    /// </summary>
    public readonly string Name;
    /// <summary>
    /// Defines the auto delete value.
    /// </summary>
    public readonly TimeSpan? AutoDelete;
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
    public ServiceBusEndpointAddress(Uri hostAddress, Uri address, AddressType type = AddressType.Queue)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentNullException.ThrowIfNull(address);

        Scheme = null!;
        Host = null!;
        Scope = null!;
        Name = null!;

        AutoDelete = default;
        Type = type;

        var scheme = address.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case "sb":
                Scheme = address.Scheme;
                Host = address.Host;

                address.ParseHostPathAndEntityName(out Scope, out Name);
                break;

            case "queue":
                ParseLeft(hostAddress, out Scheme, out Host, out Scope);

                Name = address.AbsolutePath;
                break;

            case "topic":
                ParseLeft(new Uri(hostAddress.GetLeftPart(UriPartial.Authority)), out Scheme, out Host, out Scope);

                Name = address.AbsolutePath;
                Type = AddressType.Topic;
                break;

            default:
                throw new ArgumentException($"The address scheme is not supported: {address.Scheme}", nameof(address));
        }

        foreach (var (key, value) in address.SplitQueryString())
        {
            switch (key)
            {
                case AutoDeleteKey when int.TryParse(value, out var result):
                    AutoDelete = TimeSpan.FromSeconds(result);
                    break;

                case TypeKey when _parseConverter.TryConvert(value, out var result):
                    Type = result;
                    break;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="type">The type value.</param>
    public ServiceBusEndpointAddress(Uri hostAddress, string name, TimeSpan? autoDelete = default, AddressType type = AddressType.Queue)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The Azure Service Bus entity name must not be empty.", nameof(name));

        ParseLeft(hostAddress, out Scheme, out Host, out Scope);

        Name = name;

        AutoDelete = autoDelete;
        Type = type;
    }

    /// <summary>
    /// Gets the path value.
    /// </summary>
    public string Path => Scope == "/" ? Name : $"{Scope}/{Name}";

    static void ParseLeft(Uri address, out string scheme, out string host, out string scope)
    {
        var hostAddress = new ServiceBusHostAddress(address);
        scheme = hostAddress.Scheme;
        host = hostAddress.Host;
        scope = hostAddress.Scope;
    }

    /// <summary>
    /// Converts a value to <see cref="Uri" />.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator Uri(in ServiceBusEndpointAddress address)
    {
        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host,
            Path = address.Scope == "/" || address.Name.IndexOf('/') > 0
                ? $"/{address.Name}"
                : $"/{address.Scope}/{address.Name}"
        };

        builder.Query += string.Join("&", address.GetQueryStringOptions());

        return builder.Uri;
    }

    Uri DebuggerDisplay => this;

    IEnumerable<string> GetQueryStringOptions()
    {
        if (AutoDelete.HasValue && AutoDelete.Value != Defaults.AutoDeleteOnIdle)
            yield return $"{AutoDeleteKey}={AutoDelete.Value.TotalSeconds:F0}";

        if (Type != AddressType.Queue)
            yield return $"{TypeKey}=topic";
    }
}
