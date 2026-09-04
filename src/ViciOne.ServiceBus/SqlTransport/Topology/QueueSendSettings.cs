using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a queue send settings implementation.
/// </summary>
public class QueueSendSettings :
    SqlQueueConfigurator,
    SendSettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public QueueSendSettings(SqlEndpointAddress address)
        : base(address.Name, address.AutoDeleteOnIdle)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="queueName">The queue name value.</param>
    public QueueSendSettings(EntitySettings settings, string queueName)
        : base(queueName, settings.AutoDeleteOnIdle)
    {
    }

    /// <summary>
    /// Gets send address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public SqlEndpointAddress GetSendAddress(Uri hostAddress)
    {
        return new SqlEndpointAddress(hostAddress, QueueName, AutoDeleteOnIdle);
    }

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.CreateQueue(QueueName, AutoDeleteOnIdle);

        return builder.BuildBrokerTopology();
    }

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public string EntityName => QueueName;
}
