namespace ViciOne.ServiceBus
{
    using System;


    public class OutboxDeliveryServiceOptions<TScope>
        where TScope : class
    {
        public int MessageDeliveryLimit { get; set; } = 100;
        public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan QueryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public int QueryMessageLimit { get; set; } = 100;
        public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaximumDeliveryAttempts { get; set; } = 10;
        public TimeSpan InitialDeliveryRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan MaximumDeliveryRetryDelay { get; set; } = TimeSpan.FromMinutes(1);
    }
}
