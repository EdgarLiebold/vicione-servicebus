// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public class QuartzEndpointOptions
    {
        public int? PrefetchCount { get; set; } = 32;
        public int? ConcurrentMessageLimit { get; set; }
        public string QueueName { get; set; } = "quartz";
    }
}
