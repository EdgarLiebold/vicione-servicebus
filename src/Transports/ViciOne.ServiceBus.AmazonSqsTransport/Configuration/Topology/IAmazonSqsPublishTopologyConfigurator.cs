// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System;


public interface IAmazonSqsPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IAmazonSqsPublishTopology
{
    new IAmazonSqsMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    new IAmazonSqsMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
