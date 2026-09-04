using System;

namespace ViciOne.ServiceBus;

public interface IAmazonSqsPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IAmazonSqsPublishTopology
{
    new IAmazonSqsMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    new IAmazonSqsMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
