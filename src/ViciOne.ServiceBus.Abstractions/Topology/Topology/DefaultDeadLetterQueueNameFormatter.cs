// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Topology
{
    public class DefaultDeadLetterQueueNameFormatter :
        IDeadLetterQueueNameFormatter
    {
        const string DeadLetterQueueSuffix = "_skipped";

        public static readonly IDeadLetterQueueNameFormatter Instance = new DefaultDeadLetterQueueNameFormatter();

        public string FormatDeadLetterQueueName(string queueName)
        {
            return queueName + DeadLetterQueueSuffix;
        }
    }
}
