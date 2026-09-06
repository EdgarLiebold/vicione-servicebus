using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines named-options binding for RabbitMQ connection and management settings.</summary>
public sealed class RabbitMqTransportOptions
{
    const int DefaultPort = 5672;
    const int DefaultSslPort = 5671;
    const int DefaultManagementPort = 15672;
    const int DefaultSslManagementPort = 443;

    bool _useSsl;

    /// <summary>Creates options with local-development connection defaults.</summary>
    public RabbitMqTransportOptions()
    {
        Host = HostMetadataCache.IsRunningInContainer ? "rabbitmq" : "localhost";
        Port = DefaultPort;
        ManagementPort = DefaultManagementPort;
        VHost = "/";
        User = "guest";
        Pass = "guest";
    }

    /// <summary>Gets or sets the RabbitMQ host name.</summary>
    public string Host { get; set; }
    /// <summary>Gets or sets the AMQP port.</summary>
    public ushort Port { get; set; }
    /// <summary>Gets or sets the RabbitMQ management API port.</summary>
    public ushort ManagementPort { get; set; }
    /// <summary>Gets or sets the RabbitMQ virtual host.</summary>
    public string VHost { get; set; }
    /// <summary>Gets or sets the RabbitMQ user name.</summary>
    public string User { get; set; }
    /// <summary>Gets or sets the RabbitMQ password.</summary>
    public string Pass { get; set; }
    /// <summary>Gets or sets the client-provided connection name shown by RabbitMQ.</summary>
    public string? ConnectionName { get; set; }

    /// <summary>Gets or sets whether AMQP and management connections use TLS.</summary>
    public bool UseSsl
    {
        get => _useSsl;
        set
        {
            _useSsl = value;

            if (!_useSsl)
                return;

            if (Port == DefaultPort)
                Port = DefaultSslPort;

            if (ManagementPort == DefaultManagementPort)
                ManagementPort = DefaultSslManagementPort;
        }
    }
}
