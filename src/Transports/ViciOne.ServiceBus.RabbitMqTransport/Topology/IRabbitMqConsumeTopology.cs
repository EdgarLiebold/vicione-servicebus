using ViciOne.ServiceBus.RabbitMqTransport.Topology;

namespace ViciOne.ServiceBus;

public interface IRabbitMqConsumeTopology :
    IConsumeTopology
{
    IExchangeTypeSelector ExchangeTypeSelector { get; }

    new IRabbitMqMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Apply the entire topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
