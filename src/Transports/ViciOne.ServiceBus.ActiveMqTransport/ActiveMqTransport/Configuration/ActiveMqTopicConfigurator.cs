using ViciOne.ServiceBus.ActiveMqTransport.Topology;

namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration;

public class ActiveMqTopicConfigurator :
    EntityConfigurator,
    IActiveMqTopicConfigurator,
    Topic
{
    public ActiveMqTopicConfigurator(string topicName, bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
    }

    public ActiveMqTopicConfigurator(Topic source)
        : base(source.EntityName, source.Durable, source.AutoDelete)
    {
    }

    protected override ActiveMqEndpointAddress.AddressType AddressType => ActiveMqEndpointAddress.AddressType.Topic;
}
