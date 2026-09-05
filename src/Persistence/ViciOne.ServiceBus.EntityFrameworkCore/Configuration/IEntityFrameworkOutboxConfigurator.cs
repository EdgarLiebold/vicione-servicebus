using System;
using System.Data;
using ViciOne.ServiceBus.EntityFrameworkCore;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Defines the contract for entity framework outbox configurator.
/// </summary>
public interface IEntityFrameworkOutboxConfigurator :
    ITransactionalOutboxConfigurator
{
    /// <summary>
    /// The amount of time a message remains in the inbox for duplicate detection (based on MessageId)
    /// </summary>
    public TimeSpan DuplicateDetectionWindow { set; }

    /// <summary>
    /// Gets or sets the isolation level value.
    /// </summary>
    IsolationLevel IsolationLevel { set; }

    /// <summary>
    /// Gets or sets the lock statement provider value.
    /// </summary>
    ILockStatementProvider LockStatementProvider { set; }

    /// <summary>
    /// The delay between queries once messages are no longer available. When a query returns messages, subsequent queries
    /// are performed until no messages are returned after which the QueryDelay is used.
    /// </summary>
    public TimeSpan QueryDelay { set; }

    /// <summary>
    /// The maximum number of messages to query from the database at a time
    /// </summary>
    public int QueryMessageLimit { set; }

    /// <summary>
    /// Database query timeout
    /// </summary>
    public TimeSpan QueryTimeout { set; }

    /// <summary>
    /// Disable the inbox cleanup service, removing the hosted service from the service collection
    /// </summary>
    void DisableInboxCleanupService();

    /// <summary>
    /// The Bus Outbox intercepts the <see cref="ISendEndpointProvider" /> and <see cref="IPublishEndpoint" /> interfaces
    /// that are used when not consuming messages. Messages sent or published via those interfaces are written to the outbox
    /// instead of being delivered directly to the message broker.
    /// </summary>
    void EnableTransactionalOutbox(Action<IEntityFrameworkBusOutboxConfigurator>? configure = null);
}
