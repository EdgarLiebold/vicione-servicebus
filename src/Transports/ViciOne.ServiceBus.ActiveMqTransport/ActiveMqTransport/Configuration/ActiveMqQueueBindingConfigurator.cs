// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration
{
    public class ActiveMqQueueBindingConfigurator :
        ActiveMqQueueConfigurator,
        IActiveMqQueueBindingConfigurator
    {
        protected ActiveMqQueueBindingConfigurator(string queueName, bool durable, bool autoDelete)
            : base(queueName, durable, autoDelete)
        {
        }

        public string Selector { get; set; }
    }
}
