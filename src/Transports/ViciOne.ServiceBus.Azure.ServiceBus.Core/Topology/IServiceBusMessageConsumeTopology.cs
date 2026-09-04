namespace ViciOne.ServiceBus;

public interface IServiceBusMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
