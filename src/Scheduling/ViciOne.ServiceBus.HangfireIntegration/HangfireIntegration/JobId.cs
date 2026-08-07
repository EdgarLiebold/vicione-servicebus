// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.HangfireIntegration
{
    internal static class JobKey
    {
        public static string Create(string scheduleId, string? scheduleGroup)
        {
            return string.IsNullOrEmpty(scheduleGroup) ? scheduleId : $"{scheduleId}-{scheduleGroup}";
        }
    }
}
