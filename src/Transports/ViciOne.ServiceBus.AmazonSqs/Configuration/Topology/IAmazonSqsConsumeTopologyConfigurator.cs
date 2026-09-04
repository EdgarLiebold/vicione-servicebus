namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs consume topology configurator.
/// </summary>
public interface IAmazonSqsConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IAmazonSqsConsumeTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IAmazonSqsMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    void AddSpecification(IAmazonSqsConsumeTopologySpecification specification);
}
