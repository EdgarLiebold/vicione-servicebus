using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using ViciOne.ServiceBus.Internals;

#nullable enable
namespace ViciOne.ServiceBus;

[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct RabbitMqHostAddress
{
    public const string RabbitMqScheme = "rabbitmq";
    public const string RabbitMqSecureScheme = "rabbitmqs";

    public RabbitMqHostAddress(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        Scheme = NormalizeScheme(address.Scheme);
        Host = string.IsNullOrWhiteSpace(address.Host)
            ? throw new RabbitMqAddressException("The RabbitMQ host must not be empty.")
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
                        throw new RabbitMqAddressException($"The RabbitMQ address option '{key}' is not supported.");
                    break;
            }
        }
    }

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

    public string Scheme { get; }
    public string Host { get; }
    public int Port { get; }
    public string VirtualHost { get; }
    public ushort? Heartbeat { get; }
    public ushort? Prefetch { get; }
    public int? TimeToLive { get; }

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
        _ => throw new RabbitMqAddressException($"The address scheme is not supported: {scheme}")
    };

    internal static string NormalizeScheme(string scheme)
    {
        var normalized = scheme.ToLowerInvariant();
        return normalized switch
        {
            RabbitMqScheme or RabbitMqSecureScheme or "amqp" or "amqps" => normalized,
            _ => throw new RabbitMqAddressException($"The address scheme is not supported: {scheme}")
        };
    }

    internal static bool IsSecureScheme(string scheme) => scheme is RabbitMqSecureScheme or "amqps";

    static void EnsureSingleValue(ISet<string> seenOptions, string key)
    {
        if (!seenOptions.Add(key))
            throw new RabbitMqAddressException($"The RabbitMQ address option '{key}' must occur at most once.");
    }

    static ushort ParseUInt16(string key, string? value)
    {
        if (ushort.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
            return result;

        throw new RabbitMqAddressException($"The RabbitMQ address option '{key}' must be an unsigned 16-bit integer.");
    }

    static int ParseNonNegativeInt32(string key, string? value)
    {
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result) && result >= 0)
            return result;

        throw new RabbitMqAddressException($"The RabbitMQ address option '{key}' must be a non-negative 32-bit integer.");
    }

    static int ValidatePort(int port)
    {
        if (port is > 0 and <= 65535)
            return port;

        throw new RabbitMqAddressException("The RabbitMQ port must be between 1 and 65535.");
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
