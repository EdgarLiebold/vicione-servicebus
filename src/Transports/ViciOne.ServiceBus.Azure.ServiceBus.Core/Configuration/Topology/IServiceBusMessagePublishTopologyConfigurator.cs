// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IServiceBusMessagePublishTopologyConfigurator<TMessage> :
        IServiceBusMessagePublishTopologyConfigurator,
        IMessagePublishTopologyConfigurator<TMessage>,
        IServiceBusMessagePublishTopology<TMessage>
        where TMessage : class
    {
    }


    public interface IServiceBusMessagePublishTopologyConfigurator :
        IMessagePublishTopologyConfigurator,
        IServiceBusTopicConfigurator
    {
    }
}
