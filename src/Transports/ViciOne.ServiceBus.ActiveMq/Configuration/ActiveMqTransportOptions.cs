namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Specifies the available active mq transport protocol values.
/// </summary>
public enum ActiveMqTransportProtocol
{
    /// <summary>
    /// Indicates open wire.
    /// </summary>
    OpenWire = 0,
    /// <summary>
    /// Indicates amqp.
    /// </summary>
    Amqp = 1
}


/// <summary>
/// Defines configuration options for active mq transport.
/// </summary>
public sealed class ActiveMqTransportOptions
{
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public string? Host { get; set; }
    /// <summary>
    /// Gets or sets the protocol value.
    /// </summary>
    public ActiveMqTransportProtocol? Protocol { get; set; }
    /// <summary>
    /// Gets or sets the port value.
    /// </summary>
    public ushort? Port { get; set; }
    /// <summary>
    /// Gets or sets the use ssl value.
    /// </summary>
    public bool UseSsl { get; set; }
    /// <summary>
    /// Gets or sets the user value.
    /// </summary>
    public string? User { get; set; }
    /// <summary>
    /// Gets or sets the pass value.
    /// </summary>
    public string? Pass { get; set; }
}
