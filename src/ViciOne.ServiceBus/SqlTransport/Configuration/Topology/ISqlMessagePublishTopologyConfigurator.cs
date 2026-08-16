#nullable enable
namespace ViciOne.ServiceBus
{
    public interface ISqlMessagePublishTopologyConfigurator<TMessage> :
        IMessagePublishTopologyConfigurator<TMessage>,
        ISqlMessagePublishTopology<TMessage>,
        ISqlMessagePublishTopologyConfigurator
        where TMessage : class
    {
    }


    public interface ISqlMessagePublishTopologyConfigurator :
        IMessagePublishTopologyConfigurator,
        ISqlTopicConfigurator
    {
    }
}
