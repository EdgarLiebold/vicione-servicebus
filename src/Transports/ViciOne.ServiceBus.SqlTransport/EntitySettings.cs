using System;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines settings for entity.</summary>
public interface EntitySettings
{
    /// <summary>Gets the entity name.</summary>
    string EntityName { get; }

    /// <summary>Idle time before queue should be deleted (consumer-idle, not producer).</summary>
    TimeSpan? AutoDeleteOnIdle { get; }
}
