using System;

namespace ViciOne.ServiceBus;

public interface ISqlPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    ISqlPublishTopology
{
    new ISqlMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    new ISqlMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
