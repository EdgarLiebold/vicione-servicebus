using System;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ connection, authentication, and client behavior.</summary>
public interface IRabbitMqHostConfigurator
{
    /// <summary>
    /// Sets whether RabbitMQ publisher confirmations are enabled. When enabled, a send configured to await
    /// acknowledgement completes only after the broker confirms the publish.
    /// </summary>
    bool PublisherConfirmation { set; }

    /// <summary>Sets the callback invoked before each connection attempt to refresh client-factory settings.</summary>
    RefreshConnectionFactoryCallback OnRefreshConnectionFactory { set; }

    /// <summary>Sets the credential provider, overriding the default username/password credentials.</summary>
    ICredentialsProvider CredentialsProvider { set; }

    /// <summary>Enables TLS and optionally customizes the RabbitMQ client TLS settings.</summary>
    /// <param name="configure">An optional TLS configuration callback.</param>
    void UseSsl(Action<IRabbitMqSslConfigurator>? configure = null);

    /// <summary>
    /// Specifies the heartbeat interval, in seconds, used to maintain the connection to RabbitMQ.
    /// Setting this value to zero will disable heartbeats, allowing the connection to timeout
    /// after an inactivity period.
    /// </summary>
    /// <param name="requestedHeartbeat">The requested heartbeat interval in seconds.</param>
    void Heartbeat(ushort requestedHeartbeat);

    /// <summary>
    /// Specifies the heartbeat interval, used to maintain the connection to RabbitMQ.
    /// Setting this value to TimeSpan.Zero will disable heartbeats, allowing the connection to timeout
    /// after an inactivity period.
    /// </summary>
    /// <param name="timeSpan">The requested heartbeat interval.</param>
    void Heartbeat(TimeSpan timeSpan);

    /// <summary>Sets the username for the connection to RabbitMQ.</summary>
    /// <param name="username">The RabbitMQ user name.</param>
    void Username(string username);

    /// <summary>Sets the password for the connection to RabbitMQ.</summary>
    /// <param name="password">The RabbitMQ password.</param>
    void Password(string password);

    /// <summary>Configures cluster nodes that the client can use when establishing a connection.</summary>
    /// <param name="configureCluster">The callback that adds cluster nodes.</param>
    void UseCluster(Action<IRabbitMqClusterConfigurator> configureCluster);

    /// <summary>Sets the requested maximum number of channels per connection.</summary>
    /// <param name="value">The requested AMQP channel limit.</param>
    void RequestedChannelMax(ushort value);

    /// <summary>Sets the requested maximum AMQP frame size.</summary>
    /// <param name="value">The requested frame-size limit in bytes.</param>
    void RequestedFrameMax(uint value);

    /// <summary>Sets the requested connection timeout in milliseconds.</summary>
    /// <param name="milliseconds">The connection timeout in milliseconds.</param>
    void RequestedConnectionTimeout(int milliseconds);

    /// <summary>Sets the requested connection timeout.</summary>
    /// <param name="timeSpan">The connection timeout.</param>
    void RequestedConnectionTimeout(TimeSpan timeSpan);

    /// <summary>Sets the continuation timeout for command communication with RabbitMQ.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    void ContinuationTimeout(TimeSpan timeout);

    /// <summary>Sets the maximum inbound message-body size accepted by RabbitMQ.Client.</summary>
    /// <param name="maxMessageSize">The maximum body size in bytes.</param>
    void MaxMessageSize(uint maxMessageSize);

    /// <summary>Sets the client-provided connection name shown by RabbitMQ.</summary>
    /// <param name="connectionName">The client-provided connection name, or <see langword="null"/> to omit it.</param>
    void ConnectionName(string? connectionName);
}
