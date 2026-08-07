// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IMessagePublishTopologyConvention<TMessage> :
        IMessagePublishTopologyConvention
        where TMessage : class
    {
        bool TryGetMessagePublishTopology(out IMessagePublishTopology<TMessage> messagePublishTopology);
    }


    public interface IMessagePublishTopologyConvention
    {
        bool TryGetMessagePublishTopologyConvention<T>(out IMessagePublishTopologyConvention<T> convention)
            where T : class;
    }
}
