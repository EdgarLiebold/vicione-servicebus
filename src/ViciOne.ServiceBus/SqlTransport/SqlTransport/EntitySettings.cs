// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport
{
    using System;


    public interface EntitySettings
    {
        string EntityName { get; }

        /// <summary>
        /// Idle time before queue should be deleted (consumer-idle, not producer)
        /// </summary>
        TimeSpan? AutoDeleteOnIdle { get; }
    }
}
