using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.MessageJournal;

#nullable enable
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="contextOptions">The context options value.</param>
    /// <param name="tableName">The table name value.</param>
    /// <param name="limits">The limits value.</param>
    /// <param name="schemaName">The schema name value.</param>
    public EntityFrameworkMessageJournalStore(
        DbContextOptions contextOptions,
        string tableName,
        MessageJournalStoreLimits limits,
        string? schemaName = null)
    {
        ArgumentNullException.ThrowIfNull(contextOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentNullException.ThrowIfNull(limits);
        if (schemaName is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        _contextOptions = contextOptions;
        _tableName = tableName;
        _schemaName = schemaName;
        Limits = limits;
    }

    /// <summary>
    /// Gets the limits value.
    /// </summary>
    public MessageJournalStoreLimits Limits { get; }

    /// <summary>
    /// Performs the append operation.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
