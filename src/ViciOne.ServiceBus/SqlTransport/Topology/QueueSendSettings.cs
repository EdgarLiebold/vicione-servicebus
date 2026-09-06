using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines settings for queue send.</summary>
public class QueueSendSettings :
    SqlQueueConfigurator,
    SendSettings
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="address">The address.</param>
    public QueueSendSettings(SqlEndpointAddress address)
        : base(address.Name, address.AutoDeleteOnIdle)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    /// <param name="queueName">The queue name.</param>
    public QueueSendSettings(EntitySettings settings, string queueName)
        : base(queueName, settings.AutoDeleteOnIdle)
    {
    }

    /// <summary>Gets send address.</summary>
    /// <param name="hostAddress">The host address.</param>
    /// <returns>The send address.</returns>
    public SqlEndpointAddress GetSendAddress(Uri hostAddress)
    {
        return new SqlEndpointAddress(hostAddress, QueueName, AutoDeleteOnIdle);
    }

    /// <summary>Gets broker topology.</summary>
    /// <returns>The broker topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.CreateQueue(QueueName, AutoDeleteOnIdle);

        return builder.BuildBrokerTopology();
    }

    /// <summary>Gets the entity name.</summary>
    public string EntityName => QueueName;
}
