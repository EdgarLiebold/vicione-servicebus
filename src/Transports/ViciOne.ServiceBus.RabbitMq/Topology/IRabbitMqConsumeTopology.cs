using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq consume topology.
/// </summary>
public interface IRabbitMqConsumeTopology :
    IConsumeTopology
{
    /// <summary>
    /// Gets the exchange type selector value.
    /// </summary>
    IExchangeTypeSelector ExchangeTypeSelector { get; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IRabbitMqMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Apply the entire topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
