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
