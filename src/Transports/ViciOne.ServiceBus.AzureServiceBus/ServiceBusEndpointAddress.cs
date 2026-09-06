using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Parses and formats Azure Service Bus queue or topic addresses.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct ServiceBusEndpointAddress
{
    const string AutoDeleteKey = "autodelete";
    const string TypeKey = "type";


    /// <summary>Identifies the Azure entity addressed by an endpoint URI.</summary>
    public enum AddressType
    {
        /// <summary>The address identifies a queue.</summary>
        Queue = 0,
        /// <summary>The address identifies a topic.</summary>
        Topic = 1
    }


    static readonly ITypeConverter<AddressType, string> _parseConverter = new EnumTypeConverter<AddressType>();

    /// <summary>The Azure Service Bus URI scheme.</summary>
    public readonly string Scheme;
    /// <summary>The Azure Service Bus namespace host.</summary>
    public readonly string Host;
    /// <summary>The optional namespace-relative scope preceding the entity name.</summary>
    public readonly string Scope;

    /// <summary>The queue or topic name.</summary>
    public readonly string Name;
    /// <summary>The optional idle interval after which the entity is deleted.</summary>
    public readonly TimeSpan? AutoDelete;
    /// <summary>The addressed entity kind.</summary>
    public readonly AddressType Type;

    /// <summary>Resolves an endpoint URI against an Azure Service Bus namespace address.</summary>
    /// <param name="hostAddress">The namespace address used for relative queue or topic URIs.</param>
    /// <param name="address">The absolute or transport-relative endpoint address.</param>
    /// <param name="type">The default entity kind when the address does not override it.</param>
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

    /// <summary>Creates an endpoint address from a namespace address and entity name.</summary>
    /// <param name="hostAddress">The Azure Service Bus namespace address.</param>
    /// <param name="name">The namespace-relative entity name.</param>
    /// <param name="autoDelete">The optional idle interval after which the entity is deleted.</param>
    /// <param name="type">The entity kind.</param>
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

    /// <summary>Gets the namespace-relative entity path, including its scope.</summary>
    public string Path => Scope == "/" ? Name : $"{Scope}/{Name}";

    static void ParseLeft(Uri address, out string scheme, out string host, out string scope)
    {
        var hostAddress = new ServiceBusHostAddress(address);
        scheme = hostAddress.Scheme;
        host = hostAddress.Host;
        scope = hostAddress.Scope;
    }

    /// <summary>Formats the endpoint address and its non-default options as a URI.</summary>
    /// <param name="address">The endpoint address to format.</param>
    /// <returns>The absolute Azure Service Bus endpoint URI.</returns>
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
