namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures the Amazon SNS topic used to publish a message type.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IAmazonSqsMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    IAmazonSqsMessagePublishTopology<TMessage>,
    IAmazonSqsMessagePublishTopologyConfigurator
    where TMessage : class
{
}


/// <summary>Configures untyped Amazon SNS message publish topology and topic settings.</summary>
public interface IAmazonSqsMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator,
    IAmazonSqsMessagePublishTopology,
    IAmazonSqsTopicConfigurator
{
}
