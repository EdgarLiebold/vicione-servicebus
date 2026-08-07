// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public class HangfireEndpointOptions
    {
        public const string DefaultQueueName = "vicione-servicebus-message-queue";

        public int? PrefetchCount { get; set; } = Environment.ProcessorCount;
        public int? ConcurrentMessageLimit { get; set; }
        public string QueueName { get; set; } = "hangfire";
    }
}
