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
    /// <param name="key">The incoming-message and consumer identity.</param>
    /// <param name="now">The authoritative acquisition time.</param>
    /// <param name="leaseDuration">The exclusive processing duration.</param>
    /// <param name="cancellationToken">The token used to cancel acquisition.</param>
    /// <returns>A task containing the durable duplicate, timing, or ownership outcome.</returns>
    Task<ReliableInboxAcquireResult> AcquireAsync(
        ReliableInboxKey key,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a leased consumer attempt committed.</summary>
    /// <param name="key">The incoming-message and consumer identity.</param>
    /// <param name="lease">The fencing lease held by the completing pipeline.</param>
    /// <param name="consumedAt">The successful completion timestamp.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing whether this lease committed the completion.</returns>
    Task<bool> CompleteAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Persists a retry instead of relying on a volatile delay.</summary>
    /// <param name="key">The incoming-message and consumer identity.</param>
    /// <param name="lease">The fencing lease held by the failed pipeline.</param>
    /// <param name="dueAt">The earliest time at which another attempt may acquire the record.</param>
    /// <param name="failureType">The optional runtime failure type retained as payload-free evidence.</param>
    /// <param name="failedAt">The failure timestamp.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing whether this lease scheduled the retry.</returns>
    Task<bool> ScheduleRetryAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        DateTimeOffset dueAt,
        string? failureType,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Moves a leased inbox record to quarantine.</summary>
    /// <param name="key">The incoming-message and consumer identity.</param>
    /// <param name="lease">The fencing lease held by the failed pipeline.</param>
    /// <param name="failureType">The optional runtime failure type retained as payload-free evidence.</param>
    /// <param name="quarantinedAt">The quarantine timestamp.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing whether this lease quarantined the record.</returns>
    Task<bool> QuarantineAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        string? failureType,
        DateTimeOffset quarantinedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one bounded seek-paginated inbox-quarantine page.</summary>
    /// <param name="query">The bounded seek-pagination query.</param>
    /// <param name="cancellationToken">The token used to cancel retrieval.</param>
    /// <returns>A task containing one payload-free quarantine page.</returns>
    Task<ReliableInboxQuarantinePage> GetQuarantineAsync(
        ReliableInboxQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Requeues a quarantined inbox record.</summary>
    /// <param name="key">The quarantined inbox identity.</param>
    /// <param name="dueAt">The earliest time at which processing may resume.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing the exact state-transition result.</returns>
    Task<ReliableMessagingOperationResult> RequeueAsync(
        ReliableInboxKey key,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default);

    /// <summary>Discards a quarantined inbox record.</summary>
    /// <param name="key">The quarantined inbox identity.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing the exact state-transition result.</returns>
    Task<ReliableMessagingOperationResult> DiscardAsync(
        ReliableInboxKey key,
        CancellationToken cancellationToken = default);

    /// <summary>Retains a quarantined record as abandoned terminal evidence.</summary>
    /// <param name="key">The quarantined inbox identity.</param>
    /// <param name="abandonedAt">The operator-decision timestamp.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing the exact state-transition result.</returns>
    Task<ReliableMessagingOperationResult> AbandonAsync(
        ReliableInboxKey key,
        DateTimeOffset abandonedAt,
        CancellationToken cancellationToken = default);
}
