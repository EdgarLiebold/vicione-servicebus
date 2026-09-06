using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Applies and validates one RabbitMQ publish-topology fragment.</summary>
public interface IRabbitMqPublishTopologySpecification :
    ISpecification
{
    /// <summary>Adds this publish-topology fragment to a broker-topology builder.</summary>
    /// <param name="builder">The publish-endpoint topology builder.</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
