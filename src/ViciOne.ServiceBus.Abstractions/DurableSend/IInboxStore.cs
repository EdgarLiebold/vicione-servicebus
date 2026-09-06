using System;
using System.Threading;
using System.Threading.Tasks;


namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Persistence SPI for duplicate-safe, retryable consumer processing.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IInboxStore<TBus>
    where TBus : class, IBus
{
    /// <summary>Acquires a due inbox record or reports its durable duplicate/ownership state.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="now">The now.</param>
    /// <param name="leaseDuration">The lease duration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the acquire outcome.</returns>
    Task<ReliableInboxAcquireResult> AcquireAsync(
        ReliableInboxKey key,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a leased consumer attempt committed.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="lease">The lease.</param>
    /// <param name="consumedAt">The consumed at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the complete outcome.</returns>
    Task<bool> CompleteAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Persists a retry instead of relying on a volatile delay.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="lease">The lease.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="failureType">The runtime failure type used by the operation.</param>
    /// <param name="failedAt">The failed at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule retry outcome.</returns>
    Task<bool> ScheduleRetryAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        DateTimeOffset dueAt,
        string? failureType,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Moves a leased inbox record to quarantine.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="lease">The lease.</param>
    /// <param name="failureType">The runtime failure type used by the operation.</param>
    /// <param name="quarantinedAt">The quarantined at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the quarantine outcome.</returns>
    Task<bool> QuarantineAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        string? failureType,
        DateTimeOffset quarantinedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one bounded seek-paginated inbox-quarantine page.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<ReliableInboxQuarantinePage> GetQuarantineAsync(
        ReliableInboxQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Requeues a quarantined inbox record.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requeue outcome.</returns>
    Task<ReliableMessagingOperationResult> RequeueAsync(
        ReliableInboxKey key,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default);

    /// <summary>Discards a quarantined inbox record.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the discard outcome.</returns>
    Task<ReliableMessagingOperationResult> DiscardAsync(
        ReliableInboxKey key,
        CancellationToken cancellationToken = default);

    /// <summary>Retains a quarantined record as abandoned terminal evidence.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="abandonedAt">The abandoned at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the abandon outcome.</returns>
    Task<ReliableMessagingOperationResult> AbandonAsync(
        ReliableInboxKey key,
        DateTimeOffset abandonedAt,
        CancellationToken cancellationToken = default);
}
