using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines settings for topic send.</summary>
public class TopicSendSettings :
    SqlTopicConfigurator,
    SendSettings
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="address">The address.</param>
    public TopicSendSettings(SqlEndpointAddress address)
        : base(address.Name)
    {
    }

    /// <summary>Gets send address.</summary>
    /// <param name="hostAddress">The host address.</param>
    /// <returns>The send address.</returns>
    public SqlEndpointAddress GetSendAddress(Uri hostAddress)
    {
        return new SqlEndpointAddress(hostAddress, TopicName);
    }

    /// <summary>Gets broker topology.</summary>
    /// <returns>The broker topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.Topic = builder.CreateTopic(TopicName);

        return builder.BuildBrokerTopology();
    }

    /// <summary>Gets the entity name.</summary>
    public string EntityName => TopicName;

    /// <summary>Gets the auto delete on idle.</summary>
    public TimeSpan? AutoDeleteOnIdle => default;
}
