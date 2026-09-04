using System;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql topic configurator implementation.
/// </summary>
public class SqlTopicConfigurator :
    ISqlTopicConfigurator,
    Topic
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    public SqlTopicConfigurator(string topicName)
    {
        TopicName = topicName;
    }

    /// <summary>
    /// Gets the topic name value.
    /// </summary>
    public string TopicName { get; }

    /// <summary>
    /// Gets endpoint address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public SqlEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new SqlEndpointAddress(hostAddress, TopicName, type: SqlEndpointAddress.AddressType.Topic);
    }
}
