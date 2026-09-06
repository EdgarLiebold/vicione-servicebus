using System;
using System.Collections.Generic;
using System.Diagnostics;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Represents an in memory endpoint address.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct InMemoryEndpointAddress
{
    const string BindQueueKey = "bind";
    const string QueueNameKey = "queue";
    const string ExchangeTypeKey = "type";

    /// <summary>Exposes the scheme used by the containing type.</summary>
    public readonly string Scheme;
    /// <summary>Exposes the host used by the containing type.</summary>
    public readonly string Host;
    /// <summary>Exposes the virtual host used by the containing type.</summary>
    public readonly string VirtualHost = null!;

    /// <summary>Exposes the name used by the containing type.</summary>
    public readonly string Name;
    /// <summary>Exposes the bind to queue used by the containing type.</summary>
    public readonly bool BindToQueue;
    /// <summary>Exposes the queue name used by the containing type.</summary>
    public readonly string? QueueName;
    /// <summary>Exposes the exchange type used by the containing type.</summary>
    public readonly ExchangeType ExchangeType;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostAddress">The host address.</param>
    /// <param name="address">The address.</param>
    public InMemoryEndpointAddress(Uri hostAddress, Uri address)
    {
        Scheme = null!;
        Host = null!;
        VirtualHost = null!;

        BindToQueue = false;
        QueueName = default;
        ExchangeType = ExchangeType.FanOut;

        var scheme = address.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case "loopback":
                Scheme = address.Scheme;
                Host = address.Host;

                address.ParseHostPathAndEntityName(out VirtualHost, out Name);
                break;

            case "queue":
                ParseLeft(hostAddress, out Scheme, out Host, out VirtualHost);

                Name = address.AbsolutePath;
                BindToQueue = true;
                break;

            case "exchange":
            case "topic":
                ParseLeft(hostAddress, out Scheme, out Host, out VirtualHost);

                Name = address.AbsolutePath;
                break;

            default:
                throw new ArgumentException($"The address scheme is not supported: {address.Scheme}", nameof(address));
        }

        if (Name == "*")
            Name = NewId.Next().ToString("NS");

        foreach (var (key, value) in address.SplitQueryString())
        {
            switch (key)
            {
                case BindQueueKey when bool.TryParse(value, out var result):
                    BindToQueue = result;
                    break;

                case QueueNameKey when !string.IsNullOrWhiteSpace(value):
                    QueueName = Uri.UnescapeDataString(value);
                    break;

                case ExchangeTypeKey when Enum.TryParse<ExchangeType>(value, out var result):
                    ExchangeType = result;
                    break;
            }
        }
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostAddress">The host address.</param>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="bindToQueue">The bind to queue.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    public InMemoryEndpointAddress(Uri hostAddress, string exchangeName, bool bindToQueue = false, string? queueName = default,
        ExchangeType exchangeType = ExchangeType.FanOut)
    {
        ParseLeft(hostAddress, out Scheme, out Host, out VirtualHost);

        Name = exchangeName;
        ExchangeType = exchangeType;

        BindToQueue = bindToQueue;
        QueueName = queueName;
    }

    static void ParseLeft(Uri address, out string scheme, out string host, out string virtualHost)
    {
        var hostAddress = new InMemoryHostAddress(address);
        scheme = hostAddress.Scheme;
        host = hostAddress.Host;
        virtualHost = hostAddress.VirtualHost;
    }

    /// <summary>Converts a value to <see cref="Uri" />.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The value produced by the operation.</returns>
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

    IEnumerable<string> GetQueryStringOptions()
    {
        if (BindToQueue)
            yield return $"{BindQueueKey}=true";
        if (!string.IsNullOrWhiteSpace(QueueName))
            yield return $"{QueueNameKey}={Uri.EscapeDataString(QueueName)}";
        if (ExchangeType != ExchangeType.FanOut)
            yield return $"{ExchangeTypeKey}={ExchangeType}";
    }
}
