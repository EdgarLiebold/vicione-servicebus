namespace ViciOne.ServiceBus;

public interface IInMemoryMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
