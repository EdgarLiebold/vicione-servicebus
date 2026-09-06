using System;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Provides the complete settings required to create an ActiveMQ connection.</summary>
public interface ActiveMqHostSettings
{
    /// <summary>Gets the broker host name.</summary>
    string Host { get; }

    /// <summary>Gets the broker port.</summary>
    int Port { get; }

    /// <summary>Gets the logical broker namespace used in service-bus endpoint addresses.</summary>
    string VirtualHost { get; }

    /// <summary>Gets the broker user name.</summary>
    string Username { get; }

    /// <summary>Gets the broker password.</summary>
    string Password { get; }

    /// <summary>Gets the canonical service-bus host address without credentials or native provider options.</summary>
    Uri HostAddress { get; }

    /// <summary>Gets whether the native provider uses TLS.</summary>
    bool UseSsl { get; }

    /// <summary>Gets the native Apache NMS connection URI.</summary>
    Uri BrokerAddress { get; }

    /// <summary>Creates an Apache NMS connection using these settings.</summary>
    /// <returns>The unstarted native connection.</returns>
    IConnection CreateConnection();
}
