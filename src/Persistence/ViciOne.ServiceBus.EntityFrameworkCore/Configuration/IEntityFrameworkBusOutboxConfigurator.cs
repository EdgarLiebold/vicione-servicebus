using System;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Configures delivery of messages persisted by an Entity Framework Core transactional outbox.</summary>
public interface IEntityFrameworkBusOutboxConfigurator :
    IBusOutboxConfigurator
{
    /// <summary>Sets the maximum number of persisted messages sent from one outbox row per delivery pass.</summary>
    int MessageDeliveryLimit { set; }
    /// <summary>Gets or sets the timeout applied to each individual transport send.</summary>
    TimeSpan MessageDeliveryTimeout { get; set; }
    /// <summary>Gets or sets the number of failed attempts after which an outbox row is quarantined.</summary>
    int MaximumDeliveryAttempts { get; set; }
    /// <summary>Gets or sets the delay before the first retry of a failed transport send.</summary>
    TimeSpan InitialDeliveryRetryDelay { get; set; }
    /// <summary>Gets or sets the upper bound for exponentially increasing delivery retry delays.</summary>
    TimeSpan MaximumDeliveryRetryDelay { get; set; }

    /// <summary>
    /// Selects this DbContext as the default outbox for untyped scoped publish/send when a bus has multiple EF outboxes.
    /// DbContext-specific transactional APIs do not require a default.
    /// </summary>
    void UseAsDefault();
}
