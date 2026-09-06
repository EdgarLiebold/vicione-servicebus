namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures Amazon SQS send topology for a message type.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IAmazonSqsMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    IAmazonSqsMessageSendTopology<TMessage>,
    IAmazonSqsMessageSendTopologyConfigurator
    where TMessage : class
{
}


/// <summary>Configures untyped Amazon SQS message send topology.</summary>
public interface IAmazonSqsMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
