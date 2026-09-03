namespace ViciOne.ServiceBus
{
    using System;


    public class QuartzEndpointOptions
    {
        public int? PrefetchCount { get; set; } = 32;
        public int? ConcurrentMessageLimit { get; set; }
        public string QueueName { get; set; } = "quartz";
        public Func<string, TimeZoneInfo?>? TimeZoneResolver { get; set; }
    }
}
