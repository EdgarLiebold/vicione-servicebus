// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Topology
{
    public class RabbitMqDelaySettings :
        RabbitMqSendSettings,
        DelaySettings
    {
        public RabbitMqDelaySettings(RabbitMqEndpointAddress address)
            : base(address)
        {
        }
    }
}
