using System;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Settings to configure a ActiveMQ host explicitly without requiring the fluent interface
/// </summary>
public interface ActiveMqHostSettings
{
    /// <summary>
    /// The ActiveMQ host to connect to (should be a valid hostname)
    /// </summary>
    string Host { get; }

    /// <summary>
    /// The ActiveMQ port to connect
    /// </summary>
    int Port { get; }

    /// <summary>
    /// The logical host scope used when formatting endpoint addresses.
    /// </summary>
    string VirtualHost { get; }

    /// <summary>
    /// The Username for connecting to the host
    /// </summary>
    string Username { get; }

    /// <summary>
    /// The password for connection to the host
    /// MAYBE this should be a SecureString instead of a regular string
    /// </summary>
    string Password { get; }

    /// <summary>
    /// Returns the host address
    /// </summary>
    Uri HostAddress { get; }

    /// <summary>
    /// Gets the use ssl value.
    /// </summary>
    bool UseSsl { get; }

    /// <summary>
    /// Gets the broker address value.
    /// </summary>
    Uri BrokerAddress { get; }

    /// <summary>
    /// Creates connection.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IConnection CreateConnection();
}
