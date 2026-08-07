// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IActiveMqMessagePublishTopologyConfigurator<TMessage> :
        IMessagePublishTopologyConfigurator<TMessage>,
        IActiveMqMessagePublishTopology<TMessage>,
        IActiveMqMessagePublishTopologyConfigurator
        where TMessage : class
    {
    }


    public interface IActiveMqMessagePublishTopologyConfigurator :
        IMessagePublishTopologyConfigurator,
        IActiveMqMessagePublishTopology,
        IActiveMqTopicConfigurator
    {
    }
}
