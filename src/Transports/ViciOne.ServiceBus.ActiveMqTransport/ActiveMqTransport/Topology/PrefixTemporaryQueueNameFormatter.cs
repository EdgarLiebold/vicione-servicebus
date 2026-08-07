// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.ActiveMqTransport.Topology
{
    public class PrefixTemporaryQueueNameFormatter :
        IActiveMqTemporaryQueueNameFormatter
    {
        readonly string _prefix;

        public PrefixTemporaryQueueNameFormatter(string prefix)
        {
            _prefix = prefix;
        }

        public string Format(string queueName)
        {
            return $"{_prefix}{queueName}";
        }
    }
}
