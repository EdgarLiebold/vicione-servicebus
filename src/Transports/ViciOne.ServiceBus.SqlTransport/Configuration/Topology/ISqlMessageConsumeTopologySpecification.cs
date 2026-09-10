using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Applies one message type's SQL subscriptions to a receive endpoint topology.</summary>
internal interface ISqlMessageConsumeTopologySpecification : IMessageConsumeTopologyConfigurator
{
    /// <summary>Adds the message subscriptions to the endpoint topology builder.</summary>
    /// <param name="builder">The endpoint topology builder.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
