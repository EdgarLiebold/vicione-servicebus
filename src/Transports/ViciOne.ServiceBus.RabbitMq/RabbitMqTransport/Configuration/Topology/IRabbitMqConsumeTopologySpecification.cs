using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Applies and validates one RabbitMQ consume-topology fragment.</summary>
public interface IRabbitMqConsumeTopologySpecification :
    ISpecification
{
    /// <summary>Adds this consume-topology fragment to a broker-topology builder.</summary>
    /// <param name="builder">The receive-endpoint topology builder.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
