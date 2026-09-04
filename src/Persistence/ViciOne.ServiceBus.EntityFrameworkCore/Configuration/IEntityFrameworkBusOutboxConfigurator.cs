using System;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Defines the contract for entity framework bus outbox configurator.
/// </summary>
public interface IEntityFrameworkBusOutboxConfigurator :
    IBusOutboxConfigurator
{
    /// <summary>
    /// Gets or sets the message delivery limit value.
    /// </summary>
    int MessageDeliveryLimit { set; }
    /// <summary>
    /// Gets or sets the message delivery timeout value.
    /// </summary>
    TimeSpan MessageDeliveryTimeout { get; set; }
    /// <summary>
    /// Gets or sets the maximum delivery attempts value.
    /// </summary>
    int MaximumDeliveryAttempts { get; set; }
    /// <summary>
    /// Gets or sets the initial delivery retry delay value.
    /// </summary>
    TimeSpan InitialDeliveryRetryDelay { get; set; }
    /// <summary>
    /// Gets or sets the maximum delivery retry delay value.
    /// </summary>
    TimeSpan MaximumDeliveryRetryDelay { get; set; }

    /// <summary>
    /// Selects this DbContext as the default outbox for untyped scoped publish/send when a bus has multiple EF outboxes.
    /// DbContext-specific transactional APIs do not require a default.
    /// </summary>
    void UseAsDefault();
}
