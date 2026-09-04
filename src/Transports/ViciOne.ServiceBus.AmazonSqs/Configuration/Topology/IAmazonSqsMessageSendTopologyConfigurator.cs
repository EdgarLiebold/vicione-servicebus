namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs message send topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IAmazonSqsMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    IAmazonSqsMessageSendTopology<TMessage>,
    IAmazonSqsMessageSendTopologyConfigurator
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for amazon sqs message send topology configurator.
/// </summary>
public interface IAmazonSqsMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
