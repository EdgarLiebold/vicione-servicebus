using ViciOne.ServiceBus.ActiveMqTransport.Configuration;

namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

public class ActiveMqDeadLetterSettings :
    ActiveMqQueueBindingConfigurator,
    DeadLetterSettings
{
    public ActiveMqDeadLetterSettings(EntitySettings source, string queueName)
        : base(queueName, source.Durable, source.AutoDelete)
    {
    }

    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.CreateQueue(EntityName, Durable, AutoDelete);

        return builder.BuildBrokerTopology();
    }
}
