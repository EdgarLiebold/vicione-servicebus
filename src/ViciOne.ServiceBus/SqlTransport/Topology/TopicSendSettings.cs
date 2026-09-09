using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Describes a topic destination and the topology required to publish to it.</summary>
public class TopicSendSettings :
    SqlTopicConfigurator,
    SendSettings
{
    /// <summary>Creates settings for the topic identified by <paramref name="address" />.</summary>
    /// <param name="address">The topic address.</param>
    public TopicSendSettings(SqlEndpointAddress address)
        : base(address.Name)
    {
    }

    /// <summary>Builds the fully qualified topic address on the specified SQL transport host.</summary>
    /// <param name="hostAddress">The SQL transport host address.</param>
    /// <returns>The fully qualified topic address.</returns>
    public SqlEndpointAddress GetSendAddress(Uri hostAddress)
    {
        return new SqlEndpointAddress(hostAddress, TopicName, type: SqlEndpointAddress.AddressType.Topic);
    }

    /// <summary>Builds the topology that declares this topic as the publish destination.</summary>
    /// <returns>The topic declaration topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.Topic = builder.CreateTopic(TopicName);

        return builder.BuildBrokerTopology();
    }

    /// <summary>Gets the destination topic name.</summary>
    public string EntityName => TopicName;

    /// <summary>Gets no idle-deletion interval because SQL transport topics are not auto-deleted.</summary>
    public TimeSpan? AutoDeleteOnIdle => default;
}
