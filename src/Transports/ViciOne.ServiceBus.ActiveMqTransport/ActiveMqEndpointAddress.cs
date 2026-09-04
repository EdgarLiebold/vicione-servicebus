using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.ActiveMqTransport.Topology;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct ActiveMqEndpointAddress
{
    const string AutoDeleteKey = "autodelete";
    const string DurableKey = "durable";
    const string TemporaryKey = "temporary";
    const string TypeKey = "type";


    public enum AddressType
    {
        Queue = 0,
        Topic = 1
    }


    static readonly ITypeConverter<AddressType, string> _parseConverter = new EnumTypeConverter<AddressType>();

    public readonly string Scheme;
    public readonly string Host;
    public readonly int? Port;
    public readonly string VirtualHost;

    public readonly string Name;
    public readonly bool Durable;
    public readonly bool AutoDelete;
    public readonly AddressType Type;

    public ActiveMqEndpointAddress(Uri hostAddress, Uri address)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentNullException.ThrowIfNull(address);

        Scheme = null!;
        Host = null!;
        Port = default;
        VirtualHost = null!;
        Name = null!;

        Durable = true;
        AutoDelete = false;
        Type = AddressType.Queue;

        var scheme = address.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case ActiveMqHostAddress.AmqpScheme:
            case ActiveMqHostAddress.ActiveMqScheme:
                ParseLeft(hostAddress, out Scheme, out Host, out Port, out var configuredVirtualHost);
                address.ParseHostPathAndEntityName(out var addressVirtualHost, out Name);
                ValidateFullAddressHost(address, Scheme, Host, Port, configuredVirtualHost, addressVirtualHost);
                VirtualHost = configuredVirtualHost;
                break;

            case "queue":
                ParseLeft(hostAddress, out Scheme, out Host, out Port, out VirtualHost);

                Name = Uri.UnescapeDataString(address.AbsolutePath);
                break;

            case "topic":
                ParseLeft(hostAddress, out Scheme, out Host, out Port, out VirtualHost);

                Name = Uri.UnescapeDataString(address.AbsolutePath);
                Type = AddressType.Topic;
                break;

            default:
                throw new ActiveMqTransportConfigurationException($"The address scheme is not supported: {address.Scheme}");
        }

        if (Name == "*")
            Name = NewId.Next().ToString("NS");

        ActiveMqEntityNameValidator.Validator.ThrowIfInvalidEntityName(Name);

        var seenOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasTemporary = false;
        var hasDurability = false;
        var hasAutoDelete = false;

        foreach (var (key, value) in address.SplitQueryString())
        {
            switch (key)
            {
                case TemporaryKey:
                    EnsureSingleValue(seenOptions, key);
                    RejectTemporaryConflict(hasDurability || hasAutoDelete);
                    hasTemporary = true;
                    var result = ParseBoolean(key, value);
                    AutoDelete = result;
                    Durable = !result;
                    break;

                case DurableKey:
                    EnsureSingleValue(seenOptions, key);
                    RejectTemporaryConflict(hasTemporary);
                    hasDurability = true;
                    Durable = ParseBoolean(key, value);
                    break;

                case AutoDeleteKey:
                    EnsureSingleValue(seenOptions, key);
                    RejectTemporaryConflict(hasTemporary);
                    hasAutoDelete = true;
                    AutoDelete = ParseBoolean(key, value);
                    break;

                case TypeKey:
                    EnsureSingleValue(seenOptions, key);
                    Type = ParseAddressType(key, value);
                    break;

                default:
                    throw new ActiveMqTransportConfigurationException($"The ActiveMQ address option '{key}' is not supported.");
            }
        }

        if (scheme == "queue" && Type != AddressType.Queue)
            throw new ActiveMqTransportConfigurationException("A queue address cannot declare the topic address type.");
        if (scheme == "topic" && Type != AddressType.Topic)
            throw new ActiveMqTransportConfigurationException("A topic address cannot declare the queue address type.");
    }

    public ActiveMqEndpointAddress(Uri hostAddress, string exchangeName, bool durable = true, bool autoDelete = false, AddressType type = AddressType.Queue)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        ParseLeft(hostAddress, out Scheme, out Host, out Port, out VirtualHost);

        ActiveMqEntityNameValidator.Validator.ThrowIfInvalidEntityName(exchangeName);
        Name = exchangeName;

        Durable = durable;
        AutoDelete = autoDelete;
        Type = type;
    }

    static void ParseLeft(Uri address, out string scheme, out string host, out int? port, out string virtualHost)
    {
        var hostAddress = new ActiveMqHostAddress(address);
        scheme = hostAddress.Scheme;
        host = hostAddress.Host;
        port = hostAddress.Port;
        virtualHost = hostAddress.VirtualHost;
    }

    static void EnsureSingleValue(ISet<string> seenOptions, string key)
    {
        if (!seenOptions.Add(key))
            throw new ActiveMqTransportConfigurationException($"The ActiveMQ address option '{key}' must occur at most once.");
    }

    static void ValidateFullAddressHost(
        Uri address,
        string configuredScheme,
        string configuredHost,
        int? configuredPort,
        string configuredVirtualHost,
        string addressVirtualHost)
    {
        if (!string.IsNullOrEmpty(address.UserInfo) || !string.IsNullOrEmpty(address.Fragment))
        {
            throw new ActiveMqTransportConfigurationException(
                "Credentials and URI fragments are not valid ActiveMQ destination address components.");
        }

        if (address.IsDefaultPort || address.Port <= 0)
            throw new ActiveMqTransportConfigurationException("A full ActiveMQ destination address must include the configured port explicitly.");

        var addressPort = address.Port;
        if (!string.Equals(address.Scheme, configuredScheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(address.Host, configuredHost, StringComparison.OrdinalIgnoreCase)
            || addressPort != configuredPort
            || !string.Equals(addressVirtualHost, configuredVirtualHost, StringComparison.Ordinal))
        {
            Uri configuredAddress = new ActiveMqHostAddress(configuredScheme, configuredHost, configuredPort, configuredVirtualHost);
            throw new ActiveMqTransportConfigurationException(
                $"The destination address '{address}' does not belong to the configured ActiveMQ host '{configuredAddress}'.");
        }
    }

    static bool ParseBoolean(string key, string? value)
    {
        if (bool.TryParse(value, out var result))
            return result;

        throw new ActiveMqTransportConfigurationException($"The ActiveMQ address option '{key}' must be either true or false.");
    }

    static AddressType ParseAddressType(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && _parseConverter.TryConvert(value, out var result))
            return result;

        throw new ActiveMqTransportConfigurationException($"The ActiveMQ address option '{key}' must be either queue or topic.");
    }

    static void RejectTemporaryConflict(bool conflict)
    {
        if (conflict)
        {
            throw new ActiveMqTransportConfigurationException(
                "The ActiveMQ address option 'temporary' must not be combined with 'durable' or 'autodelete'.");
        }
    }

    public static implicit operator Uri(in ActiveMqEndpointAddress address)
    {
        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host,
            Port = address.Port ?? throw new ActiveMqTransportConfigurationException("The ActiveMQ port is unavailable."),
            Path = address.VirtualHost == "/"
                ? $"/{Uri.EscapeDataString(address.Name)}"
                : $"/{Uri.EscapeDataString(address.VirtualHost)}/{Uri.EscapeDataString(address.Name)}"
        };

        builder.Query += string.Join("&", address.GetQueryStringOptions());

        return builder.Uri;
    }

    Uri DebuggerDisplay => this;

    public Uri TopicAddress
    {
        get
        {
            var value = $"topic:{Uri.EscapeDataString(Name)}";
            var query = string.Join("&", GetQueryStringOptions());
            return new Uri(query.Length == 0 ? value : $"{value}?{query}");
        }
    }

    IEnumerable<string> GetQueryStringOptions()
    {
        if (!Durable && AutoDelete)
            yield return $"{TemporaryKey}=true";
        else if (!Durable)
            yield return $"{DurableKey}=false";
        else if (AutoDelete)
            yield return $"{AutoDeleteKey}=true";

        if (Type != AddressType.Queue)
            yield return $"{TypeKey}=topic";
    }
}
