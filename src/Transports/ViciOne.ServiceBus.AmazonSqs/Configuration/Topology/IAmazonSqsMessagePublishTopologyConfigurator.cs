namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs message publish topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IAmazonSqsMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    IAmazonSqsMessagePublishTopology<TMessage>,
    IAmazonSqsMessagePublishTopologyConfigurator
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for amazon sqs message publish topology configurator.
/// </summary>
public interface IAmazonSqsMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator,
    IAmazonSqsMessagePublishTopology,
    IAmazonSqsTopicConfigurator
{
}
