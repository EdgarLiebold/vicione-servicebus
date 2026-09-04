using System;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public class SqlTopicConfigurator :
    ISqlTopicConfigurator,
    Topic
{
    public SqlTopicConfigurator(string topicName)
    {
        TopicName = topicName;
    }

    public string TopicName { get; }

    public SqlEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new SqlEndpointAddress(hostAddress, TopicName, type: SqlEndpointAddress.AddressType.Topic);
    }
}
