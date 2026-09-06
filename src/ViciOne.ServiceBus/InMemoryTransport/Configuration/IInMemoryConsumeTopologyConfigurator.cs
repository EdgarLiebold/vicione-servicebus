using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures in memory consume topology.</summary>
public interface IInMemoryConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IInMemoryConsumeTopology
{
    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    new IInMemoryMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Adds specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    void AddSpecification(IInMemoryConsumeTopologySpecification specification);

    /// <summary>Binds the configured entities.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    void Bind(string exchangeName, ExchangeType exchangeType = ExchangeType.FanOut, string? routingKey = default);
}
