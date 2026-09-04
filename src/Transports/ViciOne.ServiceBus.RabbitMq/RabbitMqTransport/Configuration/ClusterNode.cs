using System;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Represents a cluster node value.
/// </summary>
public readonly struct ClusterNode
{
    /// <summary>
    /// Defines the host name value.
    /// </summary>
    public readonly string HostName;
    /// <summary>
    /// Defines the port value.
    /// </summary>
    public readonly int? Port;

    ClusterNode(string hostName, int? port = default)
    {
        HostName = hostName;
        Port = port;
    }

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return Port == -1 ? HostName : $"{HostName}:{Port}";
    }

    /// <summary>
    /// Parses the supplied representation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public static ClusterNode Parse(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentNullException(nameof(address), "Address must not be null or empty");

        var elements = address.Split(':');

        return elements.Length switch
        {
            1 => new ClusterNode(elements[0]),
            2 when int.TryParse(elements[1], out var port) => new ClusterNode(elements[0], port),
            _ => throw new ArgumentException($"Invalid node address: {address}", nameof(address))
        };
    }
}
