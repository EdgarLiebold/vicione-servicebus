using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Represents an event hub endpoint address.</summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct EventHubEndpointAddress
{
    /// <summary>Identifies Event Hubs paths in bus endpoint addresses.</summary>
    public const string PathPrefix = "event-hub";

    /// <summary>The Event Hub entity name.</summary>
    public readonly string EventHubName;

    /// <summary>The Event Hubs namespace host.</summary>
    public readonly string Host;
    /// <summary>The scheme inherited from the configured host address.</summary>
    public readonly string Scheme;
    /// <summary>The explicit namespace port, when present.</summary>
    public readonly int? Port;

    /// <summary>Parses an Event Hub endpoint address relative to the configured namespace address.</summary>
    /// <param name="hostAddress">The configured Event Hubs namespace address.</param>
    /// <param name="address">The endpoint address to parse.</param>
    public EventHubEndpointAddress(Uri hostAddress, Uri address)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        ArgumentNullException.ThrowIfNull(address);

        Host = null!;
        EventHubName = null!;
        Scheme = null!;
        Port = default;

        var scheme = address.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case "topic":
                ParseLeft(hostAddress, out Scheme, out Host, out Port);
                EventHubName = address.AbsolutePath;
                break;
            default:
                {
                    if (string.Equals(address.Scheme, hostAddress.Scheme, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ParseLeft(hostAddress, out Scheme, out Host, out Port);
                        EventHubName = address.AbsolutePath.Replace($"{PathPrefix}/", "");
                    }
                    else
                        throw new ArgumentException($"The address scheme is not supported: {address.Scheme}", nameof(address));

                    break;
                }
        }
    }

    /// <summary>Creates an endpoint address for an Event Hub in the configured namespace.</summary>
    /// <param name="hostAddress">The configured Event Hubs namespace address.</param>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    public EventHubEndpointAddress(Uri hostAddress, string eventHubName)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);
        if (string.IsNullOrWhiteSpace(eventHubName))
            throw new ArgumentException("The Event Hub name must not be empty.", nameof(eventHubName));

        ParseLeft(hostAddress, out Scheme, out Host, out Port);

        EventHubName = eventHubName;
    }

    static void ParseLeft(Uri address, out string scheme, out string host, out int? port)
    {
        scheme = address.Scheme;
        host = address.Host;
        port = address.Port;
    }

    /// <summary>Converts the endpoint address to its bus URI representation.</summary>
    /// <param name="address">The Event Hubs endpoint address.</param>
    /// <returns>A URI containing the namespace and Event Hub entity path.</returns>
    public static implicit operator Uri(in EventHubEndpointAddress address)
    {
        var builder = new UriBuilder
        {
            Scheme = address.Scheme,
            Host = address.Host,
            Port = address.Port ?? 0,
            Path = $"{PathPrefix}/{address.EventHubName}"
        };

        return builder.Uri;
    }

    Uri DebuggerDisplay => this;
}
