// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.HangfireIntegration
{
    public static class HangfireMessageHeaders
    {
        public const string Sent = "ViciOne-ServiceBus-Hangfire-Sent";
        public const string Scheduled = "ViciOne-ServiceBus-Hangfire-Scheduled";
        public const string TriggerKey = "ViciOne-ServiceBus-Hangfire-TriggerKey";
    }
}
