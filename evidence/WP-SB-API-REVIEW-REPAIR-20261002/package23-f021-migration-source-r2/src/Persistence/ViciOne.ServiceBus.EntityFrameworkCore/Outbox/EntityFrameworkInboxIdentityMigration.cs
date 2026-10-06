using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Explicitly assigns a retained Classic EF inbox consumer identity to its stable replacement.</summary>
/// <param name="LegacyConsumerId">The retained identity from the previous deployment.</param>
/// <param name="StableConsumerId">The operator-confirmed replacement identity.</param>
public sealed record InboxConsumerIdentityMigration(Guid LegacyConsumerId, Guid StableConsumerId);

/// <summary>Reports committed identity changes; this is not a delivery acknowledgement.</summary>
/// <param name="InboxRowsMigrated">The number of replaced inbox principals.</param>
/// <param name="OutboxMessagesReparented">The number of outgoing rows assigned to those replacements.</param>
public sealed record InboxIdentityMigrationResult(long InboxRowsMigrated, long OutboxMessagesReparented);

/// <summary>Calculates Classic EF inbox identities and explicitly migrates retained standard-schema records offline.</summary>
/// <remarks>Stop all writers, cleanup and delivery workers before calling. The operator supplies complete ownership assignments;
/// this helper cannot infer ownership from retained hashes. Use a dedicated fresh relational context and dispose it after the call.
/// Custom inbox columns, filters, inheritance or foreign-key relationships require an application-owned migration.</remarks>
public static class EntityFrameworkInboxIdentityMigration
{
    const int MaximumConsumerIdentities = 4096;
    const int PageSize = 128;
    static readonly string[] InboxProperties =
    [nameof(InboxState.Id), nameof(InboxState.MessageId), nameof(InboxState.ConsumerId), nameof(InboxState.LockId),
        nameof(InboxState.RowVersion), nameof(InboxState.Received), nameof(InboxState.ReceiveCount),
        nameof(InboxState.ExpirationTime), nameof(InboxState.Consumed), nameof(InboxState.Delivered), nameof(InboxState.LastSequenceNumber)];

    /// <summary>Calculates the stable identity used by Classic EF for a consumer, message, namespace and absolute endpoint.</summary>
    /// <typeparam name="TConsumer">The retained consumer contract.</typeparam>
    /// <typeparam name="TMessage">The retained message contract.</typeparam>
    /// <param name="persistenceIdentity">The configured stable bus namespace.</param>
    /// <param name="inputAddress">The exact absolute endpoint address.</param>
    /// <returns>The stable consumer identity.</returns>
    public static Guid CreateConsumerId<TConsumer, TMessage>(string persistenceIdentity, Uri inputAddress)
        where TConsumer : class
        where TMessage : class
    {
        string value = BusPersistenceIdentity<IBus>.Create(persistenceIdentity).Require("Classic EF inbox migration");
        ValidateAddress(inputAddress);
        return OutboxConsumerIdentity.Create<TConsumer, TMessage>(value, inputAddress);
    }

    /// <summary>Calculates an earlier identity using the exact runtime bus key from the previous deployment.</summary>
    /// <typeparam name="TConsumer">The previous consumer contract.</typeparam>
    /// <typeparam name="TMessage">The previous message contract.</typeparam>
    /// <param name="legacyRuntimeBusKey">The previously deployed assembly/full-type key, or default for the default bus.</param>
    /// <param name="inputAddress">The exact previously deployed absolute endpoint address.</param>
    /// <returns>The legacy consumer identity; the caller must confirm its retained ownership.</returns>
    public static Guid CreateLegacyConsumerId<TConsumer, TMessage>(string legacyRuntimeBusKey, Uri inputAddress)
        where TConsumer : class
        where TMessage : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legacyRuntimeBusKey);
        ValidateAddress(inputAddress);
        return OutboxConsumerIdentity.Create<TConsumer, TMessage>(legacyRuntimeBusKey, inputAddress);
    }

    /// <summary>Atomically replaces explicitly assigned inbox principals and reparents their outgoing messages.</summary>
    /// <param name="context">A dedicated fresh relational context with the standard Classic EF inbox/outbox model.</param>
    /// <param name="mappings">Distinct, unambiguous operator-confirmed legacy-to-stable assignments.</param>
    /// <param name="explicitlyUnchangedConsumerIds">Other retained consumer identities explicitly confirmed as unchanged.</param>
    /// <param name="cancellationToken">Cancels validation or migration. Failed work before commit is rolled back.</param>
    /// <returns>Counts of changes committed in the helper-owned transaction. An already migrated inventory can return zero.</returns>
    /// <remarks>Conflicting or incomplete inventories fail before writes. Inbox surrogate IDs and generated row versions change;
    /// progress fields and outgoing payloads are preserved. The helper never commits a caller-owned transaction.
    /// Commit or disposal errors require independent inventory verification before retrying.
    /// A complete plan of more than 4096 identities, including replacements, requires an application-owned bounded migration.</remarks>
    public static async Task<InboxIdentityMigrationResult> MigrateAsync(DbContext context,
        IEnumerable<InboxConsumerIdentityMigration> mappings, IEnumerable<Guid> explicitlyUnchangedConsumerIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(explicitlyUnchangedConsumerIds);
        if (!context.Database.IsRelational() || context.Database.CurrentTransaction is not null
            || System.Transactions.Transaction.Current is not null || context.ChangeTracker.Entries().Any())
            throw new InvalidOperationException("Inbox identity migration requires a fresh relational context without an existing or ambient transaction or tracked entities.");
        ValidateModel(context);
        Dictionary<Guid, Guid> plan = ValidatePlan(mappings, explicitlyUnchangedConsumerIds, out HashSet<Guid> allowed);
        cancellationToken.ThrowIfCancellationRequested();
        IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        InboxIdentityMigrationResult? result = null;
        Exception? primary = null;
        bool committed = false;
        try
        {
            Guid[] inventory = await context.Set<InboxState>().AsNoTracking().Select(x => x.ConsumerId).Distinct()
                .Take(MaximumConsumerIdentities + 1).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            if (inventory.Length > MaximumConsumerIdentities || inventory.Any(id => !allowed.Contains(id)))
                throw new InvalidOperationException("The retained inbox inventory is incomplete or exceeds the bounded identity limit. Explicitly assign every retained consumer identity before migration.");
            foreach ((Guid oldId, Guid newId) in plan.Where(x => x.Key != x.Value))
            {
                bool conflict = await context.Set<InboxState>().AsNoTracking().Where(x => x.ConsumerId == oldId)
                    .AnyAsync(old => context.Set<InboxState>().Any(next => next.ConsumerId == newId && next.MessageId == old.MessageId), cancellationToken)
                    .ConfigureAwait(false);
                if (conflict)
                    throw new InvalidOperationException("Legacy and stable inbox rows already exist for the same message. Resolve the ownership conflict explicitly before migration.");
            }

            long migrated = 0, reparented = 0;
            long? cursor = null;
            Guid[] sources = plan.Where(x => x.Key != x.Value).Select(x => x.Key).ToArray();
            while (sources.Length != 0)
            {
                IQueryable<InboxState> query = context.Set<InboxState>().AsNoTracking().Where(x => sources.Contains(x.ConsumerId));
                if (cursor is { } after) query = query.Where(x => x.Id > after);
                InboxState[] page = await query.OrderBy(x => x.Id).Take(PageSize)
                    .ToArrayAsync(cancellationToken).ConfigureAwait(false);
                if (page.Length == 0) break;
                foreach (InboxState old in page)
                {
                    Guid newId = plan[old.ConsumerId];
                    var replacement = new InboxState
                    {
                        MessageId = old.MessageId, ConsumerId = newId, LockId = old.LockId, Received = old.Received,
                        ReceiveCount = old.ReceiveCount, ExpirationTime = old.ExpirationTime, Consumed = old.Consumed,
                        Delivered = old.Delivered, LastSequenceNumber = old.LastSequenceNumber
                    };
                    context.Add(replacement);
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    context.Entry(replacement).State = EntityState.Detached;
                    reparented += await context.Set<OutboxMessage>()
                        .Where(x => x.InboxConsumerId == old.ConsumerId && x.InboxMessageId == old.MessageId)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.InboxConsumerId, (Guid?)newId), cancellationToken)
                        .ConfigureAwait(false);
                    int removed = await context.Set<InboxState>().Where(x => x.Id == old.Id)
                        .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                    if (removed != 1)
                        throw new InvalidOperationException("The retained inbox changed during migration. Keep all writers stopped and retry from a verified inventory.");
                    migrated++; cursor = old.Id;
                }
            }
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            committed = true;
            result = new(migrated, reparented);
        }
        catch (Exception failure) { primary = failure; }

        List<Exception> failures = [];
        if (primary is not null) failures.Add(primary);
        if (!committed)
            try { await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false); }
            catch (Exception failure) { failures.Add(failure); }
        try { await transaction.DisposeAsync().ConfigureAwait(false); }
        catch (Exception failure) { failures.Add(failure); }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(failures);
        return result ?? throw new InvalidOperationException("Inbox identity migration completed without a result.");
    }

    static Dictionary<Guid, Guid> ValidatePlan(IEnumerable<InboxConsumerIdentityMigration> mappings,
        IEnumerable<Guid> unchanged, out HashSet<Guid> allowed)
    {
        Dictionary<Guid, Guid> plan = [];
        HashSet<Guid> targets = [];
        foreach (InboxConsumerIdentityMigration mapping in mappings)
        {
            if (mapping is null || mapping.LegacyConsumerId == Guid.Empty || mapping.StableConsumerId == Guid.Empty
                || !plan.TryAdd(mapping.LegacyConsumerId, mapping.StableConsumerId) || !targets.Add(mapping.StableConsumerId)
                || plan.Count > MaximumConsumerIdentities)
                throw new ArgumentException("Inbox identity assignments must be nonempty, distinct, unambiguous and bounded.", nameof(mappings));
        }
        if (plan.Any(x => x.Key != x.Value && plan.ContainsKey(x.Value)))
            throw new ArgumentException("Inbox identity assignments cannot contain chains or cycles.", nameof(mappings));
        allowed = plan.Keys.Concat(targets).ToHashSet();
        HashSet<Guid> preserved = [];
        foreach (Guid id in unchanged)
        {
            if (id == Guid.Empty || !preserved.Add(id) || allowed.Contains(id) || allowed.Count >= MaximumConsumerIdentities)
                throw new ArgumentException("Unchanged inbox identities must be distinct, nonempty and separate from the migration assignments.", nameof(unchanged));
            allowed.Add(id);
        }
        if (allowed.Count > MaximumConsumerIdentities)
            throw new ArgumentException("The complete inbox identity plan exceeds the bounded identity limit.", nameof(mappings));
        return plan;
    }

    static void ValidateModel(DbContext context)
    {
        IEntityType inbox = context.Model.FindEntityType(typeof(InboxState))
            ?? throw new InvalidOperationException("The context has no Classic EF inbox mapping.");
        IEntityType outbox = context.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException("The context has no Classic EF outgoing-message mapping.");
        if (inbox.BaseType is not null || inbox.GetDerivedTypes().Any() || outbox.BaseType is not null || outbox.GetDerivedTypes().Any()
            || inbox.GetDeclaredQueryFilters().Any() || outbox.GetDeclaredQueryFilters().Any()
            || !inbox.GetProperties().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal)
                .SequenceEqual(InboxProperties.OrderBy(x => x, StringComparer.Ordinal))
            || inbox.FindProperty(nameof(InboxState.Id))?.ValueGenerated != ValueGenerated.OnAdd)
            throw new InvalidOperationException("Custom inbox/outbox models require an application-owned identity migration.");
        IKey? primary = inbox.FindPrimaryKey();
        IKey[] inboxKeys = inbox.GetKeys().ToArray();
        if (primary is null || !HasProperties(primary.Properties, nameof(InboxState.Id))
            || inboxKeys.Length != 2
            || !inboxKeys.Any(key => key != primary && HasProperties(key.Properties, nameof(InboxState.MessageId), nameof(InboxState.ConsumerId)))
            || inbox.GetForeignKeys().Any()
            || outbox.GetKeys().Count() != 1
            || outbox.FindPrimaryKey() is not { } outgoingPrimary
            || !HasProperties(outgoingPrimary.Properties, nameof(OutboxMessage.SequenceNumber))
            || outbox.GetReferencingForeignKeys().Any())
            throw new InvalidOperationException("Custom inbox/outbox keys or relationships require an application-owned identity migration.");

        foreach (IProperty property in inbox.GetProperties().Where(x => x.Name != nameof(InboxState.Id) && x.Name != nameof(InboxState.RowVersion)))
            if (property.ValueGenerated != ValueGenerated.Never || property.GetBeforeSaveBehavior() != PropertySaveBehavior.Save)
                throw new InvalidOperationException("Generated or ignored inbox progress fields require an application-owned identity migration.");
        IProperty rowVersion = inbox.FindProperty(nameof(InboxState.RowVersion))!;
        if (!rowVersion.IsConcurrencyToken || rowVersion.ValueGenerated != ValueGenerated.OnAddOrUpdate
            || rowVersion.GetBeforeSaveBehavior() != PropertySaveBehavior.Ignore || rowVersion.GetAfterSaveBehavior() != PropertySaveBehavior.Ignore)
            throw new InvalidOperationException("A nonstandard inbox row version requires an application-owned identity migration.");

        IForeignKey[] incoming = inbox.GetReferencingForeignKeys().ToArray();
        IForeignKey[] outgoing = outbox.GetForeignKeys().ToArray();
        if (incoming.Length != 1 || incoming[0].DeclaringEntityType != outbox
            || !HasProperties(incoming[0].Properties, nameof(OutboxMessage.InboxMessageId), nameof(OutboxMessage.InboxConsumerId))
            || !HasProperties(incoming[0].PrincipalKey.Properties, nameof(InboxState.MessageId), nameof(InboxState.ConsumerId))
            || outgoing.Length != 2 || !outgoing.Contains(incoming[0])
            || !outgoing.Any(key => key.PrincipalEntityType.ClrType == typeof(OutboxState)
                && HasProperties(key.Properties, nameof(OutboxMessage.OutboxId))
                && HasProperties(key.PrincipalKey.Properties, nameof(OutboxState.OutboxId))))
            throw new InvalidOperationException("Custom inbox/outbox foreign keys require an application-owned migration that preserves their references.");
        foreach (string name in new[] { nameof(OutboxMessage.InboxConsumerId), nameof(OutboxMessage.InboxMessageId) })
            if (outbox.FindProperty(name) is not { ValueGenerated: ValueGenerated.Never } property
                || property.GetBeforeSaveBehavior() != PropertySaveBehavior.Save)
                throw new InvalidOperationException("Generated outbox ownership fields require an application-owned identity migration.");
    }

    static bool HasProperties(IReadOnlyList<IProperty> properties, params string[] names)
        => properties.Select(x => x.Name).SequenceEqual(names);

    static void ValidateAddress(Uri inputAddress)
    {
        ArgumentNullException.ThrowIfNull(inputAddress);
        if (!inputAddress.IsAbsoluteUri)
            throw new ArgumentException("An absolute inbox endpoint address is required.", nameof(inputAddress));
    }
}
