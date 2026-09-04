namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs message send topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IAmazonSqsMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
