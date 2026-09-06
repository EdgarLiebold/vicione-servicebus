namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Identifies the native protocol used to connect to ActiveMQ.</summary>
public enum ActiveMqTransportProtocol
{
    /// <summary>Uses the ActiveMQ OpenWire protocol.</summary>
    OpenWire = 0,
    /// <summary>Uses AMQP 1.0.</summary>
    Amqp = 1
}


/// <summary>Defines options-bound ActiveMQ connection settings for bus registration.</summary>
public sealed class ActiveMqTransportOptions
{
    /// <summary>Gets or sets the broker host name.</summary>
    public string? Host { get; set; }
    /// <summary>Gets or sets the required native broker protocol.</summary>
    public ActiveMqTransportProtocol? Protocol { get; set; }
    /// <summary>Gets or sets the required broker port.</summary>
    public ushort? Port { get; set; }
    /// <summary>Gets or sets whether TLS is enabled.</summary>
    public bool UseSsl { get; set; }
    /// <summary>Gets or sets the broker user name.</summary>
    public string? User { get; set; }
    /// <summary>Gets or sets the broker password.</summary>
    public string? Pass { get; set; }
}
