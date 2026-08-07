// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Scheduling
{
    public static class ScheduleTokenId
    {
        public static void UseTokenId<T>(ScheduleTokenIdCache<T>.TokenIdSelector tokenIdSelector)
            where T : class
        {
            ScheduleTokenIdCache<T>.UseTokenId(tokenIdSelector);
        }
    }
}
