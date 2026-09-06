using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Applies database-backed message topology to a receive endpoint.</summary>
public interface IDbMessageConsumeTopologyConfigurator : IMessageConsumeTopologyConfigurator
{
    /// <summary>Applies the configured message topology to the endpoint topology builder.</summary>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
