namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs message consume topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IAmazonSqsMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
