using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a topic send settings implementation.
/// </summary>
public class TopicSendSettings :
    SqlTopicConfigurator,
    SendSettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public TopicSendSettings(SqlEndpointAddress address)
        : base(address.Name)
    {
    }

    /// <summary>
    /// Gets send address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public SqlEndpointAddress GetSendAddress(Uri hostAddress)
    {
        return new SqlEndpointAddress(hostAddress, TopicName);
    }

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.Topic = builder.CreateTopic(TopicName);

        return builder.BuildBrokerTopology();
    }

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public string EntityName => TopicName;

    /// <summary>
    /// Gets the auto delete on idle value.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle => default;
}
