using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using ViciOne.ServiceBus.Internals;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Represents a rabbit mq host address value.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct RabbitMqHostAddress
{
    /// <summary>
    /// Defines the rabbit mq scheme value.
    /// </summary>
    public const string RabbitMqScheme = "rabbitmq";
    /// <summary>
    /// Defines the rabbit mq secure scheme value.
    /// </summary>
    public const string RabbitMqSecureScheme = "rabbitmqs";

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public RabbitMqHostAddress(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        Scheme = NormalizeScheme(address.Scheme);
        Host = string.IsNullOrWhiteSpace(address.Host)
            ? throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "The RabbitMQ host must not be empty.", "Correct the named configuration before starting the host"))
            : address.Host;
        Port = address.IsDefaultPort || address.Port <= 0
            ? GetDefaultPort(Scheme)
            : ValidatePort(address.Port);
        VirtualHost = address.ParseHostPath();

        Heartbeat = null;
        Prefetch = null;
        TimeToLive = null;

        var seenOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in address.SplitQueryString())
        {
            switch (key)
            {
                case RabbitMqAddressOptionNames.Heartbeat:
                    EnsureSingleValue(seenOptions, key);
                    Heartbeat = ParseUInt16(key, value);
                    break;

                case RabbitMqAddressOptionNames.Prefetch:
                    EnsureSingleValue(seenOptions, key);
                    Prefetch = ParseUInt16(key, value);
                    break;

                case RabbitMqAddressOptionNames.TimeToLive:
                    EnsureSingleValue(seenOptions, key);
                    TimeToLive = ParseNonNegativeInt32(key, value);
                    break;

                default:
                    if (!RabbitMqAddressOptionNames.IsEndpointOption(key))
                        throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The RabbitMQ address option '{key}' is not supported.", "Correct the named configuration before starting the host"));
                    break;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="host">The host value.</param>
    /// <param name="port">The port value.</param>
    /// <param name="virtualHost">The virtual host value.</param>
    /// <param name="useTls">The use tls value.</param>
    public RabbitMqHostAddress(string? host, int? port, string? virtualHost, bool useTls = false)
    {
        Host = string.IsNullOrWhiteSpace(host)
            ? throw new ArgumentException("The RabbitMQ host must not be null, empty, or whitespace.", nameof(host))
            : host;

        Scheme = useTls ? RabbitMqSecureScheme : RabbitMqScheme;
        Port = port switch
        {
            null or 0 => useTls ? 5671 : 5672,
            < 0 or > 65535 => throw new ArgumentOutOfRangeException(nameof(port), port, "The RabbitMQ port must be between 1 and 65535, or zero for the scheme default."),
            _ => port.Value
        };
        VirtualHost = string.IsNullOrWhiteSpace(virtualHost) ? "/" : virtualHost;

        Heartbeat = null;
        Prefetch = null;
        TimeToLive = null;
    }

    /// <summary>
    /// Gets the scheme value.
    /// </summary>
    public string Scheme { get; }
    /// <summary>
    /// Gets the host value.
    /// </summary>
    public string Host { get; }
    /// <summary>
    /// Gets the port value.
    /// </summary>
    public int Port { get; }
    /// <summary>
    /// Gets the virtual host value.
    /// </summary>
    public string VirtualHost { get; }
    /// <summary>
    /// Gets the heartbeat value.
    /// </summary>
    public ushort? Heartbeat { get; }
    /// <summary>
    /// Gets the prefetch value.
    /// </summary>
    public ushort? Prefetch { get; }
    /// <summary>
    /// Gets the time to live value.
    /// </summary>
    public int? TimeToLive { get; }

    /// <summary>
    /// Converts a value to <see cref="Uri" />.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator Uri(in RabbitMqHostAddress address)
    {
        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host,
            Port = address.Port == GetDefaultPort(address.Scheme) ? -1 : address.Port,
            Path = address.VirtualHost == "/"
                ? "/"
                : $"/{Uri.EscapeDataString(address.VirtualHost)}"
        };

        builder.Query = string.Join("&", address.GetQueryStringOptions());

        return builder.Uri;
    }

    internal static int GetDefaultPort(string scheme) => scheme switch
    {
        RabbitMqSecureScheme or "amqps" => 5671,
        RabbitMqScheme or "amqp" => 5672,
        _ => throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The address scheme is not supported: {scheme}", "Correct the named configuration before starting the host"))
    };

    internal static string NormalizeScheme(string scheme)
    {
        var normalized = scheme.ToLowerInvariant();
        return normalized switch
        {
            RabbitMqScheme or RabbitMqSecureScheme or "amqp" or "amqps" => normalized,
            _ => throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The address scheme is not supported: {scheme}", "Correct the named configuration before starting the host"))
        };
    }

    internal static bool IsSecureScheme(string scheme) => scheme is RabbitMqSecureScheme or "amqps";

    static void EnsureSingleValue(ISet<string> seenOptions, string key)
    {
        if (!seenOptions.Add(key))
            throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The RabbitMQ address option '{key}' must occur at most once.", "Correct the named configuration before starting the host"));
    }

    static ushort ParseUInt16(string key, string? value)
    {
        if (ushort.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
            return result;

        throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The RabbitMQ address option '{key}' must be an unsigned 16-bit integer.", "Correct the named configuration before starting the host"));
    }

    static int ParseNonNegativeInt32(string key, string? value)
    {
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result) && result >= 0)
            return result;

        throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", $"The RabbitMQ address option '{key}' must be a non-negative 32-bit integer.", "Correct the named configuration before starting the host"));
    }

    static int ValidatePort(int port)
    {
        if (port is > 0 and <= 65535)
            return port;

        throw new RabbitMqAddressException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "The RabbitMQ port must be between 1 and 65535.", "Correct the named configuration before starting the host"));
    }

    IEnumerable<string> GetQueryStringOptions()
    {
        if (Heartbeat.HasValue)
            yield return $"{RabbitMqAddressOptionNames.Heartbeat}={Heartbeat.Value.ToString(CultureInfo.InvariantCulture)}";
        if (Prefetch.HasValue)
            yield return $"{RabbitMqAddressOptionNames.Prefetch}={Prefetch.Value.ToString(CultureInfo.InvariantCulture)}";
        if (TimeToLive.HasValue)
            yield return $"{RabbitMqAddressOptionNames.TimeToLive}={TimeToLive.Value.ToString(CultureInfo.InvariantCulture)}";
    }

    Uri DebuggerDisplay => this;
}
