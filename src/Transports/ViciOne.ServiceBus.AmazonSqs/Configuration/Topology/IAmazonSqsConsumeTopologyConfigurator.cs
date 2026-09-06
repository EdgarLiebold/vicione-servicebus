namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures Amazon SNS topic subscriptions for an Amazon SQS receive queue.</summary>
public interface IAmazonSqsConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IAmazonSqsConsumeTopology
{
    /// <summary>Gets consume topology for a message type.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <returns>The Amazon SQS message consume-topology configurator.</returns>
    new IAmazonSqsMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Adds a queue-subscription specification.</summary>
    /// <param name="specification">The specification to apply and validate.</param>
    void AddSpecification(IAmazonSqsConsumeTopologySpecification specification);
}
