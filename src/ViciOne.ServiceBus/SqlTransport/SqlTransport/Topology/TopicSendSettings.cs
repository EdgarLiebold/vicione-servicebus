// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport.Topology
{
    using System;
    using Configuration;


    public class TopicSendSettings :
        SqlTopicConfigurator,
        SendSettings
    {
        public TopicSendSettings(SqlEndpointAddress address)
            : base(address.Name)
        {
        }

        public SqlEndpointAddress GetSendAddress(Uri hostAddress)
        {
            return new SqlEndpointAddress(hostAddress, TopicName);
        }

        public BrokerTopology GetBrokerTopology()
        {
            var builder = new PublishEndpointBrokerTopologyBuilder();

            builder.Topic = builder.CreateTopic(TopicName);

            return builder.BuildBrokerTopology();
        }

        public string EntityName => TopicName;

        public TimeSpan? AutoDeleteOnIdle => default;
    }
}
