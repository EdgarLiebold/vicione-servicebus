using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines configuration options for rabbit mq transport.
/// </summary>
public class RabbitMqTransportOptions
{
    const int DefaultPort = 5672;
    const int DefaultSslPort = 5671;
    const int DefaultManagementPort = 15672;
    const int DefaultSslManagementPort = 443;

    bool _useSsl;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RabbitMqTransportOptions()
    {
        Host = HostMetadataCache.IsRunningInContainer ? "rabbitmq" : "localhost";
        Port = DefaultPort;
        ManagementPort = DefaultManagementPort;
        VHost = "/";
        User = "guest";
        Pass = "guest";
    }

    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public string Host { get; set; }
    /// <summary>
    /// Gets or sets the port value.
    /// </summary>
    public ushort Port { get; set; }
    /// <summary>
    /// Gets or sets the management port value.
    /// </summary>
    public ushort ManagementPort { get; set; }
    /// <summary>
    /// Gets or sets the v host value.
    /// </summary>
    public string VHost { get; set; }
    /// <summary>
    /// Gets or sets the user value.
    /// </summary>
    public string User { get; set; }
    /// <summary>
    /// Gets or sets the pass value.
    /// </summary>
    public string Pass { get; set; }
    /// <summary>
    /// Gets or sets the connection name value.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the use ssl value.
    /// </summary>
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
