using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

#nullable enable
namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for in memory consume topology configurator.
/// </summary>
public interface IInMemoryConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IInMemoryConsumeTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IInMemoryMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    void AddSpecification(IInMemoryConsumeTopologySpecification specification);

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    /// <param name="routingKey">The routing key value.</param>
    void Bind(string exchangeName, ExchangeType exchangeType = ExchangeType.FanOut, string? routingKey = default);
}
