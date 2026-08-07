// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public static class MessageCorrelation
    {
        public static void UseCorrelationId<T>(Func<T, Guid> getCorrelationId)
            where T : class
        {
            GlobalTopology.Send.UseCorrelationId(getCorrelationId);
        }
    }
}
