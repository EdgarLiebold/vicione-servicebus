using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Persistence SPI for duplicate-safe, retryable consumer processing.</summary>
public interface IInboxStore<TBus>
    where TBus : class, IBus
{
    /// <summary>Acquires a due inbox record or reports its durable duplicate/ownership state.</summary>
    Task<ReliableInboxAcquireResult> AcquireAsync(
        ReliableInboxKey key,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a leased consumer attempt committed.</summary>
    Task<bool> CompleteAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Persists a retry instead of relying on a volatile delay.</summary>
    Task<bool> ScheduleRetryAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        DateTimeOffset dueAt,
        string? failureType,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Moves a leased inbox record to quarantine.</summary>
    Task<bool> QuarantineAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        string? failureType,
        DateTimeOffset quarantinedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one bounded seek-paginated inbox-quarantine page.</summary>
    Task<ReliableInboxQuarantinePage> GetQuarantineAsync(
        ReliableInboxQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Requeues a quarantined inbox record.</summary>
    Task<ReliableMessagingOperationResult> RequeueAsync(
        ReliableInboxKey key,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default);

    /// <summary>Discards a quarantined inbox record.</summary>
    Task<ReliableMessagingOperationResult> DiscardAsync(
        ReliableInboxKey key,
        CancellationToken cancellationToken = default);

    /// <summary>Retains a quarantined record as abandoned terminal evidence.</summary>
    Task<ReliableMessagingOperationResult> AbandonAsync(
        ReliableInboxKey key,
        DateTimeOffset abandonedAt,
        CancellationToken cancellationToken = default);
}
