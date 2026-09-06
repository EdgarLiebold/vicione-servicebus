namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Represents RabbitMQ consume topology for one message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract type.</typeparam>
public interface IRabbitMqMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
