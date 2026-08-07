// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IActiveMqMessageSendTopologyConfigurator<TMessage> :
        IMessageSendTopologyConfigurator<TMessage>,
        IActiveMqMessageSendTopology<TMessage>,
        IActiveMqMessageSendTopologyConfigurator
        where TMessage : class
    {
    }


    public interface IActiveMqMessageSendTopologyConfigurator :
        IMessageSendTopologyConfigurator
    {
    }
}
