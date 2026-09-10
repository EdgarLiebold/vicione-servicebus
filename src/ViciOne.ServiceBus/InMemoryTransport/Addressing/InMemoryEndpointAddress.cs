using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Providers.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Addressing;

/// <summary>Parses and normalizes an in-memory exchange or queue address.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
internal readonly struct InMemoryEndpointAddress
{
    const string ExchangeTypeKey = "type";

    /// <summary>Parses an absolute loopback address or a short queue, exchange, or topic address.</summary>
    /// <param name="hostAddress">The configured in-memory host used to resolve and validate the destination.</param>
    /// <param name="address">The destination address to parse.</param>
    public InMemoryEndpointAddress(Uri hostAddress, Uri address)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentNullException.ThrowIfNull(address);

        var configuredHost = new InMemoryHostAddress(hostAddress);
        Scheme = configuredHost.Scheme;
        Host = configuredHost.Host;
        VirtualHost = configuredHost.VirtualHost;
        Name = string.Empty;

        ExchangeType = InMemoryExchangeType.FanOut;

        var addressScheme = address.Scheme.ToLowerInvariant();
        switch (addressScheme)
        {
            case InMemoryHostAddress.InMemoryScheme:
                address.ParseHostPathAndEntityName(out var addressVirtualHost, out var addressName);
                ValidateFullAddress(address, configuredHost, addressVirtualHost);
                Name = addressName;
                break;

            case "queue":
                Name = Uri.UnescapeDataString(address.AbsolutePath);
                break;

            case "exchange":
                Name = Uri.UnescapeDataString(address.AbsolutePath);
                break;

            case "topic":
                Name = Uri.UnescapeDataString(address.AbsolutePath);
                ExchangeType = InMemoryExchangeType.Topic;
                break;

            default:
                throw new ArgumentException($"The in-memory address scheme is not supported: {address.Scheme}", nameof(address));
        }

        if (Name == "*")
            Name = NewId.Next().ToString("NS");
        else if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("The in-memory endpoint address must include an entity name.", nameof(address));

        var seenOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in address.SplitQueryString())
        {
            if (!seenOptions.Add(key))
                throw new ArgumentException($"The in-memory address option '{key}' must occur at most once.", nameof(address));

            switch (key)
            {
                case ExchangeTypeKey:
                    ExchangeType = ParseExchangeType(value, address);
                    break;

                default:
                    throw new ArgumentException($"The in-memory address option '{key}' is not supported.", nameof(address));
            }
        }

        if (addressScheme == "topic" && ExchangeType != InMemoryExchangeType.Topic)
            throw new ArgumentException("A topic address cannot declare a non-topic exchange type.", nameof(address));
    }

    /// <summary>Creates an endpoint address from explicit exchange settings.</summary>
    /// <param name="hostAddress">The configured in-memory host address.</param>
    /// <param name="exchangeName">The destination exchange name.</param>
    /// <param name="exchangeType">The routing behavior of the exchange.</param>
    public InMemoryEndpointAddress(
        Uri hostAddress,
        string exchangeName,
        InMemoryExchangeType exchangeType = InMemoryExchangeType.FanOut)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeName);
        if (!Enum.IsDefined(exchangeType))
            throw new ArgumentOutOfRangeException(nameof(exchangeType), exchangeType, "The exchange type is not supported.");

        var configuredHost = new InMemoryHostAddress(hostAddress);
        Scheme = configuredHost.Scheme;
        Host = configuredHost.Host;
        VirtualHost = configuredHost.VirtualHost;

        Name = exchangeName;
        ExchangeType = exchangeType;
    }

    /// <summary>Gets the normalized <c>loopback</c> scheme.</summary>
    public string Scheme { get; }

    /// <summary>Gets the logical in-memory host name.</summary>
    public string Host { get; }

    /// <summary>Gets the decoded virtual-host path.</summary>
    public string VirtualHost { get; }

    /// <summary>Gets the destination exchange name.</summary>
    public string Name { get; }

    /// <summary>Gets the exchange routing behavior.</summary>
    public InMemoryExchangeType ExchangeType { get; }

    /// <summary>Converts the normalized endpoint address to its absolute URI.</summary>
    /// <param name="address">The normalized in-memory endpoint address.</param>
    /// <returns>The canonical loopback URI and its non-default topology options.</returns>
    public static implicit operator Uri(in InMemoryEndpointAddress address)
    {
        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host,
            Path = address.VirtualHost == "/"
                ? $"/{address.Name}"
                : $"/{Uri.EscapeDataString(address.VirtualHost)}/{address.Name}"
        };

        builder.Query += string.Join("&", address.GetQueryStringOptions());

        return builder.Uri;
    }

    Uri DebuggerDisplay => this;

    static void ValidateFullAddress(Uri address, InMemoryHostAddress configuredHost, string addressVirtualHost)
    {
        if (!string.IsNullOrEmpty(address.UserInfo) || !string.IsNullOrEmpty(address.Fragment) || !address.IsDefaultPort)
        {
            throw new ArgumentException(
                "Credentials, explicit ports, and fragments are not valid in-memory endpoint address components.",
                nameof(address));
        }

        if (!string.Equals(address.Scheme, configuredHost.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(address.Host, configuredHost.Host, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(addressVirtualHost, configuredHost.VirtualHost, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The endpoint address '{address}' does not belong to the configured in-memory host '{(Uri)configuredHost}'.",
                nameof(address));
        }
    }

    static InMemoryExchangeType ParseExchangeType(string? value, Uri address)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && Enum.TryParse(value, true, out InMemoryExchangeType result)
            && Enum.IsDefined(result)
            && string.Equals(value, Enum.GetName(result), StringComparison.OrdinalIgnoreCase))
            return result;

        throw new ArgumentException($"The in-memory address option '{ExchangeTypeKey}' has an unsupported value '{value}'.", nameof(address));
    }

    IEnumerable<string> GetQueryStringOptions()
    {
        if (ExchangeType != InMemoryExchangeType.FanOut)
            yield return $"{ExchangeTypeKey}={ExchangeType}";
    }
}
