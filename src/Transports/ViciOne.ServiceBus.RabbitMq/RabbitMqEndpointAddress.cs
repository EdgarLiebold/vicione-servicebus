using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Parses and normalizes a RabbitMQ exchange address and its topology options.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct RabbitMqEndpointAddress
{
    /// <summary>The RabbitMQ exchange type supplied by the delayed-message exchange plug-in.</summary>
    public const string DelayedMessageExchangeType = "x-delayed-message";

    /// <summary>Resolves a full or short endpoint address against a RabbitMQ host address.</summary>
    /// <param name="hostAddress">The default RabbitMQ host and virtual-host address.</param>
    /// <param name="address">The full, <c>queue:</c>, or <c>exchange:</c> endpoint address to parse.</param>
    public RabbitMqEndpointAddress(Uri hostAddress, Uri address)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentNullException.ThrowIfNull(address);

        Durable = true;
        AutoDelete = false;
        SingleActiveConsumer = false;
        ExchangeType = RabbitMQ.Client.ExchangeType.Fanout;
        BindToQueue = false;
        QueueName = null;
        DelayedType = null;
        AlternateExchange = null;
        var containsHostSettings = false;

        switch (address.Scheme.ToLowerInvariant())
        {
            case RabbitMqHostAddress.RabbitMqSecureScheme:
            case "amqps":
            case RabbitMqHostAddress.RabbitMqScheme:
            case "amqp":
                containsHostSettings = true;
                ParseHost(address, out var scheme, out var host, out var port, out var virtualHost);
                Scheme = scheme;
                Host = host;
                Port = port;
                VirtualHost = virtualHost;
                address.ParseHostPathAndEntityName(out virtualHost, out var name);
                VirtualHost = virtualHost ?? "/";
                Name = name;
                break;

            case "queue":
                ParseHost(hostAddress, out scheme, out host, out port, out virtualHost);
                Scheme = scheme;
                Host = host;
                Port = port;
                VirtualHost = virtualHost;
                Name = Uri.UnescapeDataString(address.AbsolutePath);
                BindToQueue = true;
                break;

            case "exchange":
                ParseHost(hostAddress, out scheme, out host, out port, out virtualHost);
                Scheme = scheme;
                Host = host;
                Port = port;
                VirtualHost = virtualHost;
                Name = Uri.UnescapeDataString(address.AbsolutePath);
                break;

            default:
                throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The address scheme is not supported: {address.Scheme}", "Correct the named configuration before starting the host"));
        }

        if (Name == "*")
            Name = NewId.Next().ToString("NS");

        ValidateEntityName(Name);

        var bindExchanges = new List<string>();
        var uniqueBindExchanges = new HashSet<string>(StringComparer.Ordinal);
        var seenOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasTemporary = false;
        var hasDurability = false;
        var hasAutoDelete = false;

        foreach (var (key, value) in address.SplitQueryString())
        {
            switch (key)
            {
                case RabbitMqAddressOptionNames.Temporary:
                    EnsureSingleValue(seenOptions, key);
                    RejectTemporaryConflict(hasDurability || hasAutoDelete);
                    hasTemporary = true;
                    var temporary = ParseBoolean(key, value);
                    AutoDelete = temporary;
                    Durable = !temporary;
                    break;

                case RabbitMqAddressOptionNames.Durable:
                    EnsureSingleValue(seenOptions, key);
                    RejectTemporaryConflict(hasTemporary);
                    hasDurability = true;
                    Durable = ParseBoolean(key, value);
                    break;

                case RabbitMqAddressOptionNames.AutoDelete:
                    EnsureSingleValue(seenOptions, key);
                    RejectTemporaryConflict(hasTemporary);
                    hasAutoDelete = true;
                    AutoDelete = ParseBoolean(key, value);
                    break;

                case RabbitMqAddressOptionNames.ExchangeType:
                    EnsureSingleValue(seenOptions, key);
                    ExchangeType = DecodeRequiredValue(key, value);
                    break;

                case RabbitMqAddressOptionNames.BindQueue:
                    EnsureSingleValue(seenOptions, key);
                    BindToQueue = ParseBoolean(key, value);
                    break;

                case RabbitMqAddressOptionNames.QueueName:
                    EnsureSingleValue(seenOptions, key);
                    QueueName = DecodeRequiredValue(key, value);
                    ValidateEntityName(QueueName);
                    break;

                case RabbitMqAddressOptionNames.DelayedType:
                    EnsureSingleValue(seenOptions, key);
                    DelayedType = DecodeRequiredValue(key, value);
                    ExchangeType = DelayedMessageExchangeType;
                    break;

                case RabbitMqAddressOptionNames.AlternateExchange:
                    EnsureSingleValue(seenOptions, key);
                    AlternateExchange = DecodeRequiredValue(key, value);
                    ValidateEntityName(AlternateExchange);
                    break;

                case RabbitMqAddressOptionNames.BindExchange:
                    var binding = DecodeRequiredValue(key, value);
                    ValidateEntityName(binding);
                    if (uniqueBindExchanges.Add(binding))
                        bindExchanges.Add(binding);
                    break;

                case RabbitMqAddressOptionNames.SingleActiveConsumer:
                    EnsureSingleValue(seenOptions, key);
                    SingleActiveConsumer = ParseBoolean(key, value);
                    break;

                // Full transport addresses may carry host and endpoint options in the same query.
                case RabbitMqAddressOptionNames.Heartbeat:
                case RabbitMqAddressOptionNames.Prefetch:
                case RabbitMqAddressOptionNames.TimeToLive:
                    if (!containsHostSettings)
                    {
                        throw new RabbitMqAddressException(
                            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The RabbitMQ host option '{key}' is not valid on a short endpoint address.", "Correct the named configuration before starting the host"));
                    }
                    break;

                default:
                    throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The RabbitMQ address option '{key}' is not supported.", "Correct the named configuration before starting the host"));
            }
        }

        if (DelayedType is not null)
            ExchangeType = DelayedMessageExchangeType;

        BindExchanges = new ReadOnlyCollection<string>(bindExchanges);
    }

    /// <summary>Creates an endpoint address from explicit exchange and binding settings.</summary>
    /// <param name="hostAddress">The RabbitMQ host and virtual-host address.</param>
    /// <param name="exchangeName">The destination exchange name.</param>
    /// <param name="exchangeType">The RabbitMQ exchange type, or <c>fanout</c> when omitted.</param>
    /// <param name="durable">Whether the exchange and optional queue survive broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange and optional queue when unused.</param>
    /// <param name="bindToQueue">Whether to declare and bind a queue.</param>
    /// <param name="queueName">The queue name, or the exchange name when binding without an override.</param>
    /// <param name="delayedType">The underlying exchange type for an <c>x-delayed-message</c> exchange.</param>
    /// <param name="bindExchanges">Additional exchanges to bind to the destination exchange.</param>
    /// <param name="alternateExchange">The exchange that receives otherwise unroutable messages.</param>
    /// <param name="singleActiveConsumer">Whether the optional queue uses RabbitMQ single-active-consumer semantics.</param>
    public RabbitMqEndpointAddress(Uri hostAddress, string exchangeName, string? exchangeType = null, bool durable = true,
        bool autoDelete = false, bool bindToQueue = false, string? queueName = null, string? delayedType = null,
        IEnumerable<string>? bindExchanges = null, string? alternateExchange = null, bool singleActiveConsumer = false)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        ParseHost(hostAddress, out var scheme, out var host, out var port, out var virtualHost);

        ValidateEntityName(exchangeName);
        if (queueName is not null)
            ValidateEntityName(queueName);
        if (alternateExchange is not null)
            ValidateEntityName(alternateExchange);

        Scheme = scheme;
        Host = host;
        Port = port;
        VirtualHost = virtualHost;
        Name = exchangeName;
        var validatedExchangeType = exchangeType switch
        {
            null => RabbitMQ.Client.ExchangeType.Fanout,
            _ when string.IsNullOrWhiteSpace(exchangeType) => throw new ArgumentException(
                "The exchange type must not be empty or whitespace when specified.", nameof(exchangeType)),
            _ => exchangeType
        };
        Durable = durable;
        AutoDelete = autoDelete;
        SingleActiveConsumer = singleActiveConsumer;
        BindToQueue = bindToQueue;
        QueueName = queueName;
        DelayedType = delayedType switch
        {
            null => null,
            _ when string.IsNullOrWhiteSpace(delayedType) => throw new ArgumentException(
                "The delayed exchange type must not be empty or whitespace when specified.", nameof(delayedType)),
            _ => delayedType
        };
        ExchangeType = DelayedType is null ? validatedExchangeType : DelayedMessageExchangeType;
        BindExchanges = SnapshotBindings(bindExchanges);
        AlternateExchange = alternateExchange;
    }

    RabbitMqEndpointAddress(string scheme, string host, int port, string virtualHost, string name, string exchangeType, bool durable,
        bool autoDelete, bool bindToQueue, string? queueName, string? delayedType, IEnumerable<string>? bindExchanges,
        string? alternateExchange, bool singleActiveConsumer)
    {
        ValidateEntityName(name);

        Scheme = scheme;
        Host = host;
        Port = port;
        VirtualHost = virtualHost;
        Name = name;
        ExchangeType = exchangeType;
        Durable = durable;
        AutoDelete = autoDelete;
        SingleActiveConsumer = singleActiveConsumer;
        BindToQueue = bindToQueue;
        QueueName = queueName;
        DelayedType = delayedType;
        BindExchanges = SnapshotBindings(bindExchanges);
        AlternateExchange = alternateExchange;
    }

    /// <summary>Gets the normalized RabbitMQ or AMQP URI scheme.</summary>
    public string Scheme { get; }
    /// <summary>Gets the broker host name.</summary>
    public string Host { get; }
    /// <summary>Gets the AMQP port.</summary>
    public int Port { get; }
    /// <summary>Gets the RabbitMQ virtual host.</summary>
    public string VirtualHost { get; }
    /// <summary>Gets the destination exchange name.</summary>
    public string Name { get; }
    /// <summary>Gets the declared RabbitMQ exchange type.</summary>
    public string ExchangeType { get; }
    /// <summary>Gets whether the exchange and optional queue are durable.</summary>
    public bool Durable { get; }
    /// <summary>Gets whether the exchange and optional queue are auto-delete.</summary>
    public bool AutoDelete { get; }
    /// <summary>Gets whether topology includes a queue bound to the exchange.</summary>
    public bool BindToQueue { get; }
    /// <summary>Gets whether the queue uses RabbitMQ single-active-consumer semantics.</summary>
    public bool SingleActiveConsumer { get; }
    /// <summary>Gets the explicit queue name, when one was supplied.</summary>
    public string? QueueName { get; }
    /// <summary>Gets the underlying exchange type for delayed-message routing.</summary>
    public string? DelayedType { get; }
    /// <summary>Gets the additional exchanges bound to the destination exchange.</summary>
    public IReadOnlyList<string> BindExchanges { get; }
    /// <summary>Gets the exchange used for otherwise unroutable messages.</summary>
    public string? AlternateExchange { get; }

    /// <summary>Creates delayed-exchange settings that route expired deliveries back to this endpoint.</summary>
    /// <returns>The derived RabbitMQ delay settings.</returns>
    public RabbitMqDelaySettings GetDelaySettings()
    {
        var delayExchangeName = $"{Name}_delay";
        var delayExchangeAddress = new RabbitMqEndpointAddress(
            Scheme,
            Host,
            Port,
            VirtualHost,
            delayExchangeName,
            DelayedMessageExchangeType,
            Durable,
            AutoDelete,
            false,
            null,
            RabbitMQ.Client.ExchangeType.Fanout,
            null,
            null,
            false);

        var delaySettings = new RabbitMqDelaySettings(delayExchangeAddress);
        delaySettings.BindToExchange(this);
        return delaySettings;
    }

    /// <summary>Creates a host-independent <c>queue:</c> or <c>exchange:</c> address.</summary>
    /// <returns>The short endpoint address with topology query options.</returns>
    public Uri ToShortAddress()
    {
        var builder = new StringBuilder();
        builder.Append(BindToQueue ? "queue:" : "exchange:");
        builder.Append(Name);

        var query = string.Join("&", GetQueryStringOptions().Where(option => option != $"{RabbitMqAddressOptionNames.BindQueue}=true"));
        if (query.Length > 0)
            builder.Append('?').Append(query);

        return new Uri(builder.ToString());
    }

    /// <summary>Creates a full RabbitMQ transport URI from the normalized endpoint settings.</summary>
    /// <param name="address">The endpoint settings to serialize.</param>
    /// <returns>The full RabbitMQ endpoint URI.</returns>
    public static implicit operator Uri(in RabbitMqEndpointAddress address)
    {
        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host,
            Port = address.Port == RabbitMqHostAddress.GetDefaultPort(address.Scheme) ? -1 : address.Port,
            Path = address.VirtualHost == "/"
                ? $"/{address.Name}"
                : $"/{Uri.EscapeDataString(address.VirtualHost)}/{address.Name}",
            Query = string.Join("&", address.GetQueryStringOptions())
        };

        return builder.Uri;
    }

    static string DecodeRequiredValue(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The RabbitMQ address option '{key}' requires a value.", "Correct the named configuration before starting the host"));

        var decoded = Uri.UnescapeDataString(value);
        if (string.IsNullOrWhiteSpace(decoded))
            throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The RabbitMQ address option '{key}' requires a non-empty value.", "Correct the named configuration before starting the host"));

        return decoded;
    }

    static void EnsureSingleValue(ISet<string> seenOptions, string key)
    {
        if (!seenOptions.Add(key))
            throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The RabbitMQ address option '{key}' must occur at most once.", "Correct the named configuration before starting the host"));
    }

    static bool ParseBoolean(string key, string? value)
    {
        if (bool.TryParse(value, out var result))
            return result;

        throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The RabbitMQ address option '{key}' must be either true or false.", "Correct the named configuration before starting the host"));
    }

    static void RejectTemporaryConflict(bool conflict)
    {
        if (conflict)
        {
            throw new RabbitMqAddressException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "The RabbitMQ address option 'temporary' must not be combined with 'durable' or 'autodelete'.", "Correct the named configuration before starting the host"));
        }
    }

    static void ParseHost(Uri address, out string scheme, out string host, out int port, out string virtualHost)
    {
        var hostAddress = new RabbitMqHostAddress(address);
        scheme = hostAddress.Scheme;
        host = hostAddress.Host;
        port = hostAddress.Port;
        virtualHost = hostAddress.VirtualHost;
    }

    static IReadOnlyList<string> SnapshotBindings(IEnumerable<string>? bindings)
    {
        if (bindings is null)
            return Array.Empty<string>();

        var result = new List<string>();
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binding in bindings)
        {
            ValidateEntityName(binding);
            if (unique.Add(binding))
                result.Add(binding);
        }

        return new ReadOnlyCollection<string>(result);
    }

    static void ValidateEntityName(string name)
    {
        RabbitMqEntityNameValidator.Validator.ThrowIfInvalidEntityName(name);
    }

    IEnumerable<string> GetQueryStringOptions()
    {
        if (!Durable && AutoDelete)
            yield return $"{RabbitMqAddressOptionNames.Temporary}=true";
        else if (!Durable)
            yield return $"{RabbitMqAddressOptionNames.Durable}=false";
        else if (AutoDelete)
            yield return $"{RabbitMqAddressOptionNames.AutoDelete}=true";

        if (ExchangeType != RabbitMQ.Client.ExchangeType.Fanout && DelayedType is null)
            yield return $"{RabbitMqAddressOptionNames.ExchangeType}={Uri.EscapeDataString(ExchangeType)}";

        if (BindToQueue)
            yield return $"{RabbitMqAddressOptionNames.BindQueue}=true";
        if (QueueName is not null)
            yield return $"{RabbitMqAddressOptionNames.QueueName}={Uri.EscapeDataString(QueueName)}";

        if (DelayedType is not null)
            yield return $"{RabbitMqAddressOptionNames.DelayedType}={Uri.EscapeDataString(DelayedType)}";

        if (AlternateExchange is not null)
            yield return $"{RabbitMqAddressOptionNames.AlternateExchange}={Uri.EscapeDataString(AlternateExchange)}";

        foreach (var binding in BindExchanges)
            yield return $"{RabbitMqAddressOptionNames.BindExchange}={Uri.EscapeDataString(binding)}";

        if (SingleActiveConsumer)
            yield return $"{RabbitMqAddressOptionNames.SingleActiveConsumer}=true";
    }

    Uri DebuggerDisplay => this;
}
