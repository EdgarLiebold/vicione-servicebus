// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface ISqlPublishTopologyConfigurator :
        IPublishTopologyConfigurator,
        ISqlPublishTopology
    {
        new ISqlMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
            where T : class;

        new ISqlMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
    }
}
