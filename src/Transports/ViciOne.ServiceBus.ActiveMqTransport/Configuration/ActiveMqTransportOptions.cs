#nullable enable
namespace ViciOne.ServiceBus
{
    public class ActiveMqTransportOptions
    {
        public ActiveMqTransportOptions()
        {
            Port = 61616;
        }

        public string? Host { get; set; }
        public ushort Port { get; set; }
        public bool UseSsl { get; set; }
        public string? User { get; set; }
        public string? Pass { get; set; }
    }
}
