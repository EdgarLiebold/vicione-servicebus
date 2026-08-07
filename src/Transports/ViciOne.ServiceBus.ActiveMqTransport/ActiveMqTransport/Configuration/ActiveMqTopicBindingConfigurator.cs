// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration
{
    using Topology;


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
}
