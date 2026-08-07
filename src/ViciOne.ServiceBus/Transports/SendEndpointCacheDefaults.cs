// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System;


    public static class SendEndpointCacheDefaults
    {
        static SendEndpointCacheDefaults()
        {
            Capacity = 1000;
            MinAge = TimeSpan.FromSeconds(10);
            MaxAge = TimeSpan.FromHours(24);
        }

        public static int Capacity { get; set; }
        public static TimeSpan MinAge { get; set; }
        public static TimeSpan MaxAge { get; set; }
    }
}
