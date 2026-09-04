namespace ViciOne.ServiceBus;

public interface ISqlMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
