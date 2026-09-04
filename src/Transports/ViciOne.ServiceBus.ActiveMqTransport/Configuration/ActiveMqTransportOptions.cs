#nullable enable
namespace ViciOne.ServiceBus;

public enum ActiveMqTransportProtocol
{
    OpenWire = 0,
    Amqp = 1
}


public class ActiveMqTransportOptions
{
    public string? Host { get; set; }
    public ActiveMqTransportProtocol? Protocol { get; set; }
    public ushort? Port { get; set; }
    public bool UseSsl { get; set; }
    public string? User { get; set; }
    public string? Pass { get; set; }
}
