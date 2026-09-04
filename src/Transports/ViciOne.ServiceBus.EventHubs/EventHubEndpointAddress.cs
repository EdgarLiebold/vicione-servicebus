using System;
using System.Diagnostics;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Represents an event hub endpoint address value.
/// </summary>
[DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
public readonly struct EventHubEndpointAddress
{
    /// <summary>
    /// Defines the path prefix value.
    /// </summary>
    public const string PathPrefix = "event-hub";

    /// <summary>
    /// Defines the event hub name value.
    /// </summary>
    public readonly string EventHubName;

    /// <summary>
    /// Defines the host value.
    /// </summary>
    public readonly string Host;
    /// <summary>
    /// Defines the scheme value.
    /// </summary>
    public readonly string Scheme;
    /// <summary>
    /// Defines the port value.
    /// </summary>
    public readonly int? Port;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <param name="address">The address value.</param>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <param name="eventHubName">The event hub name value.</param>
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

    /// <summary>
    /// Converts a value to <see cref="Uri" />.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
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
