// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IServiceBusMessageSendTopologyConfigurator<TMessage> :
        IMessageSendTopologyConfigurator<TMessage>,
        IServiceBusMessageSendTopology<TMessage>,
        IServiceBusMessageSendTopologyConfigurator
        where TMessage : class
    {
    }


    public interface IServiceBusMessageSendTopologyConfigurator :
        IMessageSendTopologyConfigurator
    {
    }
}
