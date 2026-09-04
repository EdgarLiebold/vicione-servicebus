using System;

namespace ViciOne.ServiceBus.SqlTransport;

public interface EntitySettings
{
    string EntityName { get; }

    /// <summary>
    /// Idle time before queue should be deleted (consumer-idle, not producer)
    /// </summary>
    TimeSpan? AutoDeleteOnIdle { get; }
}
