// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

public interface IAmazonSqsMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    IAmazonSqsMessageSendTopology<TMessage>,
    IAmazonSqsMessageSendTopologyConfigurator
    where TMessage : class
{
}


public interface IAmazonSqsMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
