// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IRabbitMqMessageSendTopologyConfigurator<TMessage> :
        IMessageSendTopologyConfigurator<TMessage>,
        IRabbitMqMessageSendTopology<TMessage>,
        IRabbitMqMessageSendTopologyConfigurator
        where TMessage : class
    {
    }


    public interface IRabbitMqMessageSendTopologyConfigurator :
        IMessageSendTopologyConfigurator
    {
    }
}
