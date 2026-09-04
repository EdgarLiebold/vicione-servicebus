using System;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for entity settings.
/// </summary>
public interface EntitySettings
{
    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    string EntityName { get; }

    /// <summary>
    /// Idle time before queue should be deleted (consumer-idle, not producer)
    /// </summary>
    TimeSpan? AutoDeleteOnIdle { get; }
}
