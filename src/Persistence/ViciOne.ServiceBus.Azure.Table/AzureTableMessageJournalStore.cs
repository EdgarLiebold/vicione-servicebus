using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Azure.Table.MessageJournal;
using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>
/// Stores sanitized journal entries in one bounded Azure Table partition. A shared ETag lease makes
/// capacity pruning and append one atomic entity-group transaction; conflicts fail the optional
/// journal attempt instead of creating a retry queue.
/// </summary>
public sealed class AzureTableMessageJournalStore : IMessageJournalStore
{
    private readonly string _partitionKey;
    private readonly TableClient _table;

    /// <summary>Creates a bounded message journal in the configured Azure Table partition.</summary>
    /// <param name="table">The caller-owned table client used for reads and atomic entity-group transactions.</param>
    /// <param name="options">The partition and finite journal limits.</param>
    /// <exception cref="ArgumentNullException"><paramref name="table"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    public AzureTableMessageJournalStore(
        TableClient table,
        AzureTableMessageJournalStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(options);

        _table = table;
        _partitionKey = options.PartitionKey;
        Limits = options.Limits;
    }

    /// <summary>Gets the capacity, entry-size, and retention limits enforced by this store.</summary>
    public MessageJournalStoreLimits Limits { get; }

    /// <summary>Prunes expired or excess rows and appends one sanitized journal entry in a single partition transaction.</summary>
    /// <param name="entry">The sanitized journal entry to persist.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when Azure Table commits the lease update, removals, and new entry atomically.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The entry exceeds a configured or Azure Table storage limit.</exception>
    /// <exception cref="InvalidOperationException">The partition contains more journal rows than one transaction can repair.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled.</exception>
    public async ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();
        if (entry.ContentSizeInBytes > Limits.MaximumEntryBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entry),
                entry.ContentSizeInBytes,
                $"The sanitized journal entry exceeds the configured {Limits.MaximumEntryBytes}-byte limit.");
        }

        MessageJournalRecord appendedRecord = MessageJournalRecord.FromEntry(entry, _partitionKey);
        MessageJournalCapacityLease lease = await GetOrCreateLeaseAsync(cancellationToken).ConfigureAwait(false);
        List<MessageJournalRecord> existing = await LoadBoundedPartitionAsync(cancellationToken).ConfigureAwait(false);
        DateTimeOffset retentionBoundary = RetentionBoundary(entry.ObservedAt, Limits.RetentionPeriod);

        MessageJournalRecord[] expired = existing
            .Where(record => record.ObservedAt < retentionBoundary)
            .ToArray();
        var removals = new Dictionary<string, MessageJournalRecord>(StringComparer.Ordinal);
        foreach (MessageJournalRecord record in expired)
            removals.Add(record.RowKey, record);

        int retainedCount = existing.Count - removals.Count;
        int additionalRemovals = retainedCount - Limits.MaximumEntries + 1;
        if (additionalRemovals > 0)
        {
            foreach (MessageJournalRecord record in existing
                         .Where(record => !removals.ContainsKey(record.RowKey))
                         .OrderBy(record => record.ObservedAt)
                         .ThenBy(record => record.EntryId)
                         .Take(additionalRemovals))
                removals.Add(record.RowKey, record);
        }

        lease.Generation = checked(lease.Generation + 1);
        var actions = new List<TableTransactionAction>(removals.Count + 2)
        {
            new(TableTransactionActionType.UpdateReplace, lease, lease.ETag),
        };
        actions.AddRange(removals.Values.Select(record =>
            new TableTransactionAction(TableTransactionActionType.Delete, record, record.ETag)));
        actions.Add(new TableTransactionAction(
            TableTransactionActionType.Add,
            appendedRecord));

        await _table.SubmitTransactionAsync(actions, cancellationToken).ConfigureAwait(false);
    }

    private async Task<MessageJournalCapacityLease> GetOrCreateLeaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            global::Azure.Response<MessageJournalCapacityLease> response = await _table
                .GetEntityAsync<MessageJournalCapacityLease>(
                    _partitionKey,
                    MessageJournalCapacityLease.RowKeyValue,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return response.Value;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            await LoadBoundedPartitionAsync(cancellationToken).ConfigureAwait(false);
            var lease = new MessageJournalCapacityLease { PartitionKey = _partitionKey };
            try
            {
                await _table.AddEntityAsync(lease, cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException conflict) when (conflict.Status == 409)
            {
                // A concurrent writer created the single lease. Read its server ETag below.
            }

            global::Azure.Response<MessageJournalCapacityLease> response = await _table
                .GetEntityAsync<MessageJournalCapacityLease>(
                    _partitionKey,
                    MessageJournalCapacityLease.RowKeyValue,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return response.Value;
        }
    }

    private async Task<List<MessageJournalRecord>> LoadBoundedPartitionAsync(CancellationToken cancellationToken)
    {
        var entries = new List<MessageJournalRecord>(AzureTableMessageJournalStoreOptions.MaximumJournalEntriesPerPartition);
        string filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {_partitionKey} and RowKey ge {MessageJournalRecord.RowKeyPrefix} and RowKey lt {MessageJournalRecord.RowKeyUpperBound}");
        AsyncPageable<MessageJournalRecord> query = _table.QueryAsync<MessageJournalRecord>(
            filter,
            maxPerPage: AzureTableMessageJournalStoreOptions.MaximumJournalEntriesPerPartition + 1,
            cancellationToken: cancellationToken);

        await foreach (MessageJournalRecord record in query.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            entries.Add(record);
            if (entries.Count > AzureTableMessageJournalStoreOptions.MaximumJournalEntriesPerPartition)
            {
                throw new InvalidOperationException(
                    "The Azure Table journal partition exceeds the maximum atomically repairable capacity; append was refused without adding another row.");
            }
        }

        return entries;
    }

    private static DateTimeOffset RetentionBoundary(DateTimeOffset observedAt, TimeSpan retentionPeriod)
    {
        TimeSpan availableHistory = observedAt - DateTimeOffset.MinValue;
        return retentionPeriod >= availableHistory
            ? DateTimeOffset.MinValue
            : observedAt - retentionPeriod;
    }
}
