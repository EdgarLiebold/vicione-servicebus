// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

public interface IAmazonSqsMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    IAmazonSqsMessagePublishTopology<TMessage>,
    IAmazonSqsMessagePublishTopologyConfigurator
    where TMessage : class
{
}


public interface IAmazonSqsMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator,
    IAmazonSqsMessagePublishTopology,
    IAmazonSqsTopicConfigurator
{
}
