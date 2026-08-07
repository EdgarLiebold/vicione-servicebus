// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IInMemoryPublishTopologyConfigurator :
        IPublishTopologyConfigurator,
        IInMemoryPublishTopology
    {
        new IInMemoryMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
            where T : class;

        new IInMemoryMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
    }
}
