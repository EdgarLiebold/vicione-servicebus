using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

#nullable enable
namespace ViciOne.ServiceBus;

public interface IInMemoryConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IInMemoryConsumeTopology
{
    new IInMemoryMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    void AddSpecification(IInMemoryConsumeTopologySpecification specification);

    void Bind(string exchangeName, ExchangeType exchangeType = ExchangeType.FanOut, string? routingKey = default);
}
