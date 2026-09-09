using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;

/// <summary>
/// Stores sanitized journal entries in a relational database while enforcing count and age bounds
/// inside one serializable transaction.
/// </summary>
public sealed class EntityFrameworkMessageJournalStore : IMessageJournalStore
{
    private readonly DbContextOptions _contextOptions;
    private readonly string? _schemaName;
    private readonly string _tableName;

    /// <summary>Initializes a bounded store for an explicitly selected relational table.</summary>
    /// <param name="contextOptions">The configured relational provider options.</param>
    /// <param name="tableName">The table that stores journal entries.</param>
    /// <param name="limits">The entry-size, count, and retention bounds.</param>
    /// <param name="schemaName">The schema, or <see langword="null"/> to use the provider default.</param>
    public EntityFrameworkMessageJournalStore(
        DbContextOptions contextOptions,
        string tableName,
        MessageJournalStoreLimits limits,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(contextOptions);
        ArgumentNullException.ThrowIfNull(limits);

        _contextOptions = contextOptions;
        _tableName = RelationalIdentifierValidator.Validate(tableName, nameof(tableName));
        _schemaName = schemaName is null
            ? null
            : RelationalIdentifierValidator.Validate(schemaName, nameof(schemaName));
        Limits = limits;
    }

    /// <summary>Gets the bounds enforced transactionally before each append.</summary>
    public MessageJournalStoreLimits Limits { get; }

    /// <summary>Removes expired or excess rows and appends one sanitized entry in a serializable transaction.</summary>
    /// <param name="entry">The sanitized journal entry to persist.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when retention, capacity enforcement, and append have committed.</returns>
    public async ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.ContentSizeInBytes > Limits.MaximumEntryBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entry),
                entry.ContentSizeInBytes,
                $"The sanitized journal entry exceeds the configured {Limits.MaximumEntryBytes}-byte limit.");
        }

        await using var context = new MessageJournalDbContext(_contextOptions, _tableName, _schemaName);
        await using IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);

        DateTimeOffset retentionBoundary = RetentionBoundary(entry.ObservedAt, Limits.RetentionPeriod);
        await context.Entries
            .Where(record => record.ObservedAt < retentionBoundary)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        int currentCount = await context.Entries.CountAsync(cancellationToken).ConfigureAwait(false);
        int entriesToRemove = currentCount - Limits.MaximumEntries + 1;
        if (entriesToRemove > 0)
        {
            IQueryable<Guid> oldestEntryIds = context.Entries
                .OrderBy(record => record.ObservedAt)
                .ThenBy(record => record.EntryId)
                .Take(entriesToRemove)
                .Select(record => record.EntryId);

            await context.Entries
                .Where(record => oldestEntryIds.Contains(record.EntryId))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        context.Entries.Add(MessageJournalRecord.FromEntry(entry));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DateTimeOffset RetentionBoundary(DateTimeOffset observedAt, TimeSpan retentionPeriod)
    {
        TimeSpan availableHistory = observedAt - DateTimeOffset.MinValue;
        return retentionPeriod >= availableHistory
            ? DateTimeOffset.MinValue
            : observedAt - retentionPeriod;
    }
}
