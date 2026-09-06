using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Exposes RabbitMQ receive bindings and per-message consume topology.</summary>
public interface IRabbitMqConsumeTopology :
    IConsumeTopology
{
    /// <summary>Gets the selector used to determine exchange types for consumed message contracts.</summary>
    IExchangeTypeSelector ExchangeTypeSelector { get; }

    /// <summary>Gets consume topology for a message contract.</summary>
    /// <typeparam name="T">The consumed message contract type.</typeparam>
    /// <returns>The RabbitMQ consume topology for <typeparamref name="T"/>.</returns>
    new IRabbitMqMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Applies all configured receive bindings to a broker-topology builder.</summary>
    /// <param name="builder">The receive-endpoint topology builder.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
