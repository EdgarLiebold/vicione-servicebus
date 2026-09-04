using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.MessageJournal;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.MessageJournal;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.MessageJournal;

public sealed class EntityFrameworkMessageJournalStoreTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-CONFIGURATION", "composition-persists-terminal-envelope")]
    public async Task ConfigurationComposition_PersistsATerminalPublishEnvelope()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync("message-journal-configuration", cancellationToken);
        await CreateJournalSchemaAsync(database, cancellationToken);
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configurator =>
            configurator.UseEntityFrameworkCoreMessageJournal(
                JournalOptions(database),
                "MessageJournal",
                PassThroughPolicy(),
                Limits(maximumEntries: 10),
                JournalOptions(),
                "journal"));

        await bus.StartAsync(cancellationToken);
        try
        {
            await bus.Publish(new JournalProbe("ef-core"), cancellationToken);

            await using MessageJournalDbContext context = CreateContext(database);
            MessageJournalRecord actual = Assert.Single(await context.Entries.AsNoTracking().ToListAsync(cancellationToken));
            Assert.Equal(MessageJournalOperation.Publish, actual.Operation);
            Assert.Contains(nameof(JournalProbe), actual.MessageTypesJson, StringComparison.Ordinal);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-PERSISTENCE", "sanitized-entry-round-trip")]
    public async Task Append_PreservesEverySanitizedFieldWithoutAnAmbientSerializer()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync("message-journal-persistence", cancellationToken);
        await CreateJournalSchemaAsync(database, cancellationToken);
        var store = CreateStore(database, Limits(maximumEntries: 10));
        MessageJournalEntry expected = Entry(
            Guid.Parse("018cc251-f400-7000-8000-000000000001"),
            new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
            body: "sanitized-body"u8.ToArray());

        await store.AppendAsync(expected, cancellationToken);

        await using var context = CreateContext(database);
        MessageJournalRecord actual = Assert.Single(await context.Entries.AsNoTracking().ToListAsync(cancellationToken));
        Assert.Equal(expected.EntryId, actual.EntryId);
        Assert.Equal(expected.ObservedAt, actual.ObservedAt);
        Assert.Equal(expected.Operation, actual.Operation);
        Assert.Equal(expected.Outcome, actual.Outcome);
        Assert.Equal(expected.DataClassification, actual.DataClassification);
        Assert.Equal(expected.ContentType, actual.ContentType);
        Assert.Equal(expected.Body.ToArray(), actual.Body);
        Assert.Equal(expected.ContentSizeInBytes, actual.ContentSizeInBytes);
        Assert.Equal(expected.MessageTypes, JsonSerializer.Deserialize<string[]>(actual.MessageTypesJson));
        Assert.Equal(expected.Metadata, JsonSerializer.Deserialize<Dictionary<string, string>>(actual.MetadataJson));
        Assert.Equal(expected.Headers, JsonSerializer.Deserialize<Dictionary<string, string>>(actual.HeadersJson));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-BOUNDS", "count-and-retention-in-one-transaction")]
    public async Task Append_AtomicallyRemovesExpiredAndOldestEntriesBeforeAddingTheNewEntry()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync("message-journal-bounds", cancellationToken);
        await CreateJournalSchemaAsync(database, cancellationToken);
        var store = CreateStore(database, Limits(maximumEntries: 2, retentionPeriod: TimeSpan.FromDays(1)));
        var now = new DateTimeOffset(2030, 1, 10, 12, 0, 0, TimeSpan.Zero);

        await store.AppendAsync(Entry(Guid.CreateVersion7(), now.AddDays(-3)), cancellationToken);
        await store.AppendAsync(Entry(Guid.CreateVersion7(), now.AddHours(-12)), cancellationToken);
        await store.AppendAsync(Entry(Guid.CreateVersion7(), now.AddHours(-6)), cancellationToken);
        await store.AppendAsync(Entry(Guid.CreateVersion7(), now), cancellationToken);

        await using var context = CreateContext(database);
        DateTimeOffset[] retained = await context.Entries.AsNoTracking()
            .OrderBy(record => record.ObservedAt)
            .Select(record => record.ObservedAt)
            .ToArrayAsync(cancellationToken);
        Assert.Equal([now.AddHours(-6), now], retained);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-CONCURRENCY", "serializable-capacity-never-exceeded")]
    public async Task ConcurrentAppends_NeverExceedTheDeclaredCapacity()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync("message-journal-concurrency", cancellationToken);
        await CreateJournalSchemaAsync(database, cancellationToken);
        var store = CreateStore(database, Limits(maximumEntries: 1));
        var observedAt = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

        Task<Exception?>[] attempts = Enumerable.Range(0, 8)
            .Select(index => TryAppendAsync(
                store,
                Entry(Guid.CreateVersion7(), observedAt.AddTicks(index)),
                cancellationToken))
            .ToArray();
        Exception?[] outcomes = await Task.WhenAll(attempts);

        await using var context = CreateContext(database);
        MessageJournalRecord[] retained = await context.Entries.AsNoTracking().ToArrayAsync(cancellationToken);
        Assert.Single(retained);
        Assert.Contains(outcomes, outcome => outcome is null);
        Assert.All(outcomes.Where(outcome => outcome is not null), outcome =>
        {
            PostgresException conflict = Assert.IsType<PostgresException>(FindPostgreSqlCause(outcome!));
            Assert.Equal(PostgresErrorCodes.SerializationFailure, conflict.SqlState);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-RETENTION", "minimum-timestamp-does-not-underflow")]
    public async Task MaximumRetention_AcceptsTheEarliestRepresentableObservation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync("message-journal-retention", cancellationToken);
        await CreateJournalSchemaAsync(database, cancellationToken);
        var store = CreateStore(database, Limits(maximumEntries: 2, retentionPeriod: TimeSpan.MaxValue));
        MessageJournalEntry entry = Entry(Guid.CreateVersion7(), DateTimeOffset.MinValue.AddDays(1));

        await store.AppendAsync(entry, cancellationToken);

        await using var context = CreateContext(database);
        Assert.Equal(entry.EntryId, Assert.Single(await context.Entries.AsNoTracking().ToListAsync(cancellationToken)).EntryId);
    }

    private static async Task<Exception?> TryAppendAsync(
        IMessageJournalStore store,
        MessageJournalEntry entry,
        CancellationToken cancellationToken)
    {
        try
        {
            await store.AppendAsync(entry, cancellationToken);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static Exception FindPostgreSqlCause(Exception exception)
    {
        Exception current = exception;
        while (current.InnerException is not null)
            current = current.InnerException;

        return current;
    }

    private static MessageJournalEntry Entry(Guid id, DateTimeOffset observedAt, byte[]? body = null) =>
        MessageJournalEntryTestFactory.Create(
            id,
            observedAt,
            MessageJournalOperation.Consume,
            MessageJournalOutcome.Faulted,
            MessageJournalDataClassification.Confidential,
            "application/json",
            ["urn:message:journal"],
            new Dictionary<string, string>(StringComparer.Ordinal) { ["tenant"] = "north" },
            new Dictionary<string, string>(StringComparer.Ordinal) { ["traceparent"] = "safe" },
            body ?? "redacted"u8.ToArray());

    private static MessageJournalStoreLimits Limits(
        int maximumEntries,
        TimeSpan? retentionPeriod = null) => new(
            maximumEntryBytes: 4096,
            maximumEntries,
            retentionPeriod ?? TimeSpan.FromDays(10));

    private static MessageJournalOptions JournalOptions() => MessageJournalOptions.ContinueMessageFlow(
        TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value,
        TimeProvider.System);

    private static IMessageJournalPolicy PassThroughPolicy() => new PassThroughJournalPolicy();

    private sealed class PassThroughJournalPolicy : IMessageJournalPolicy
    {
        public ValueTask<MessageJournalProjection?> ProjectAsync(
            MessageJournalCapture capture,
            CancellationToken cancellationToken) => ValueTask.FromResult<MessageJournalProjection?>(new MessageJournalProjection(
            MessageJournalDataClassification.Internal,
            capture.ContentType,
            capture.MessageTypes,
            capture.Metadata,
            capture.Headers,
            capture.Body));
    }

    private sealed record JournalProbe(string Source);

    private static DbContextOptions JournalOptions(PostgreSqlTestDatabase database) =>
        new DbContextOptionsBuilder()
            .UseNpgsql(database.ConnectionString)
            .Options;

    private static MessageJournalDbContext CreateContext(PostgreSqlTestDatabase database) =>
        new(JournalOptions(database), "MessageJournal", "journal");

    private static EntityFrameworkMessageJournalStore CreateStore(
        PostgreSqlTestDatabase database,
        MessageJournalStoreLimits limits) =>
        new(JournalOptions(database), "MessageJournal", limits, "journal");

    private static async Task CreateJournalSchemaAsync(
        PostgreSqlTestDatabase database,
        CancellationToken cancellationToken)
    {
        await using MessageJournalDbContext context = CreateContext(database);
        Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));
    }
}
