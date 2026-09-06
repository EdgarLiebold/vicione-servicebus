namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Builds the exchange topology required to publish a message contract.</summary>
public interface IPublishEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets or sets the exchange to which the message contract is published.</summary>
    ExchangeHandle? Exchange { get; set; }

    /// <summary>Creates a child builder for an implemented message contract.</summary>
    /// <returns>A child builder that either preserves or flattens the implemented-contract hierarchy.</returns>
    IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder();
}
