namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq message consume topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IRabbitMqMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
