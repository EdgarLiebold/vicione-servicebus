// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Topology
{
    public class DefaultErrorQueueNameFormatter :
        IErrorQueueNameFormatter
    {
        const string ErrorQueueSuffix = "_error";

        public static readonly IErrorQueueNameFormatter Instance = new DefaultErrorQueueNameFormatter();

        public string FormatErrorQueueName(string queueName)
        {
            return queueName + ErrorQueueSuffix;
        }
    }
}
