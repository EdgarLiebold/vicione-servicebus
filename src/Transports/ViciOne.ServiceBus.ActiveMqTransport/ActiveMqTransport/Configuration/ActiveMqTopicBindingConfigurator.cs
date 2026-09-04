using ViciOne.ServiceBus.ActiveMqTransport.Topology;

namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration;

public class ActiveMqTopicBindingConfigurator :
    ActiveMqTopicConfigurator,
    IActiveMqTopicBindingConfigurator
{
    public ActiveMqTopicBindingConfigurator(string topicName, bool durable = true, bool autoDelete = false, string selector = null)
        : base(topicName, durable, autoDelete)
    {
        Selector = selector;
    }

    public ActiveMqTopicBindingConfigurator(Topic topic, string selector = null)
        : base(topic)
    {
        Selector = selector;
    }

    public string Selector { get; set; }
}
