namespace ViciOne.ServiceBus;

public interface IAmazonSqsMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
