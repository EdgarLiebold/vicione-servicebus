namespace ViciOne.ServiceBus;

public interface IAmazonSqsMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
