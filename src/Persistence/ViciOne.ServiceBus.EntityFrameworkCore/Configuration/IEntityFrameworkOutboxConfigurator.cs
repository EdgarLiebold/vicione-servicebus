using System;
using System.Data;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Configures EF Core inbox deduplication and transactional-outbox persistence.</summary>
public interface IEntityFrameworkOutboxConfigurator
{
    /// <summary>Gets or sets how long a message remains in the inbox for duplicate detection.</summary>
    TimeSpan DuplicateDetectionWindow { get; set; }

    /// <summary>Gets or sets the isolation level used by inbox and outbox transactions.</summary>
    IsolationLevel IsolationLevel { get; set; }

    /// <summary>Gets or sets the provider-specific SQL used to acquire inbox and outbox locks.</summary>
    ILockStatementProvider LockStatementProvider { get; set; }

    /// <summary>
    /// The delay between queries once messages are no longer available. When a query returns messages, subsequent queries
    /// are performed until no messages are returned after which the QueryDelay is used.
    /// </summary>
    TimeSpan QueryDelay { get; set; }

    /// <summary>The maximum number of messages to query from the database at a time.</summary>
    int QueryMessageLimit { get; set; }

    /// <summary>Database query timeout.</summary>
    TimeSpan QueryTimeout { get; set; }

    /// <summary>Disable the inbox cleanup service, removing the hosted service from the service collection.</summary>
    void DisableInboxCleanupService();

    /// <summary>
    /// The Bus Outbox intercepts the <see cref="ISendEndpointProvider" /> and <see cref="IPublishEndpoint" /> interfaces
    /// that are used when not consuming messages. Messages sent or published via those interfaces are written to the outbox
    /// instead of being delivered directly to the message broker.
    /// </summary>
    /// <param name="configure">An optional callback that configures bus-outbox delivery behavior.</param>
    void EnableTransactionalOutbox(Action<IEntityFrameworkBusOutboxConfigurator>? configure = null);
}
