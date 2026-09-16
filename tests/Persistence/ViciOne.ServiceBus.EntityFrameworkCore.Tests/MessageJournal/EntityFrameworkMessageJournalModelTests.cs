using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.MessageJournal;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.MessageJournal;

public sealed class EntityFrameworkMessageJournalModelTests
{
    [Theory]
    [InlineData("sql-server")]
    [InlineData("postgresql")]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-MAPPING", "provider-neutral-relational-model")]
    public void Model_MapsTheBoundedRecordForEverySupportedRelationalProvider(string provider)
    {
        DbContextOptions options = CreateOptions(provider);
        using var context = new MessageJournalDbContext(options, "MessageJournal", "journal");

        IEntityType entity = context.Model.FindEntityType(typeof(MessageJournalRecord))
            ?? throw new InvalidOperationException("MessageJournalRecord is not mapped.");

        Assert.Equal("MessageJournal", entity.GetTableName());
        Assert.Equal("journal", entity.GetSchema());
        Assert.Equal(nameof(MessageJournalRecord.EntryId), Assert.Single(entity.FindPrimaryKey()!.Properties).Name);
        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(MessageJournalRecord.ObservedAt), nameof(MessageJournalRecord.EntryId)]));
        Assert.Equal(256, entity.FindProperty(nameof(MessageJournalRecord.ContentType))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(MessageJournalRecord.Body))!.IsNullable);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-MAPPING", "dynamic-table-and-schema-model-cache")]
    public void ModelCacheKey_PreservesEachExplicitTableAndSchemaSelection()
    {
        DbContextOptions options = CreateOptions("sql-server");
        using var first = new MessageJournalDbContext(options, "JournalOne", "north");
        using var second = new MessageJournalDbContext(options, "JournalTwo", "south");

        IEntityType firstEntity = first.Model.FindEntityType(typeof(MessageJournalRecord))!;
        IEntityType secondEntity = second.Model.FindEntityType(typeof(MessageJournalRecord))!;

        Assert.Equal(("JournalOne", "north"), (firstEntity.GetTableName(), firstEntity.GetSchema()));
        Assert.Equal(("JournalTwo", "south"), (secondEntity.GetTableName(), secondEntity.GetSchema()));
        Assert.NotSame(first.Model, second.Model);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-MAPPING", "model-cache-overloads-preserve-runtime-and-design-time-identity")]
    public void ModelCacheKey_OverloadsPreserveRuntimeAndDesignTimeIdentity()
    {
        DbContextOptions<MessageJournalDbContext> journalOptions =
            new DbContextOptionsBuilder<MessageJournalDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
        DbContextOptions<JournalProbeContext> probeOptions = new DbContextOptionsBuilder<JournalProbeContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var journalContext = new MessageJournalDbContext(journalOptions, "JournalOne", "north");
        using var probeContext = new JournalProbeContext(probeOptions);
        var cacheKeys = new MessageJournalModelCacheKeyFactory();

        Assert.Equal(cacheKeys.Create(journalContext, designTime: false), cacheKeys.Create(journalContext));
        Assert.NotEqual(cacheKeys.Create(journalContext), cacheKeys.Create(journalContext, designTime: true));
        Assert.Equal(cacheKeys.Create(probeContext, designTime: false), cacheKeys.Create(probeContext));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-BOUNDS", "store-declares-explicit-finite-limits")]
    public void Store_PreservesTheExactFiniteLimitsWithoutConnectingToADatabase()
    {
        var limits = new MessageJournalStoreLimits(4096, 25, TimeSpan.FromDays(2));

        var store = new EntityFrameworkMessageJournalStore(
            CreateOptions("postgresql"),
            "MessageJournal",
            limits);

        Assert.Same(limits, store.Limits);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-BOUNDS", "sqlite-append-enforces-retention-and-capacity")]
    public async Task Append_PersistsAndBoundsEntriesWithoutAnExternalProviderAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        DbContextOptions options = new DbContextOptionsBuilder().UseSqlite(connection).Options;
        await EnsureJournalCreatedAsync(options, cancellationToken);
        var limits = new MessageJournalStoreLimits(4096, 2, TimeSpan.FromDays(1));
        var store = new EntityFrameworkMessageJournalStore(options, "MessageJournal", limits);
        var now = new DateTimeOffset(2030, 1, 10, 12, 0, 0, TimeSpan.Zero);
        MessageJournalEntry expired = CreateEntry(Guid.Parse("00000000-0000-0000-0000-000000000001"), now.AddDays(-3));
        MessageJournalEntry oldest = CreateEntry(Guid.Parse("00000000-0000-0000-0000-000000000002"), now.AddHours(-12));
        MessageJournalEntry retained = CreateEntry(Guid.Parse("00000000-0000-0000-0000-000000000003"), now.AddHours(-6));
        MessageJournalEntry appended = CreateEntry(Guid.Parse("00000000-0000-0000-0000-000000000004"), now);

        await store.AppendAsync(expired, cancellationToken);
        await store.AppendAsync(oldest, cancellationToken);
        await store.AppendAsync(retained, cancellationToken);
        await store.AppendAsync(appended, cancellationToken);

        await using var context = new MessageJournalDbContext(options, "MessageJournal");
        MessageJournalRecord[] actual = await context.Entries.AsNoTracking()
            .OrderBy(record => record.ObservedAt)
            .ToArrayAsync(cancellationToken);
        Assert.Equal([retained.EntryId, appended.EntryId], actual.Select(record => record.EntryId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-RETENTION", "sqlite-append-removes-expired-without-capacity-pressure")]
    public async Task Append_RemovesExpiredEntriesWithoutCapacityPressureAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        DbContextOptions options = new DbContextOptionsBuilder().UseSqlite(connection).Options;
        await EnsureJournalCreatedAsync(options, cancellationToken);
        var store = new EntityFrameworkMessageJournalStore(
            options,
            "MessageJournal",
            new MessageJournalStoreLimits(4096, 10, TimeSpan.FromDays(1)));
        var now = new DateTimeOffset(2030, 1, 10, 12, 0, 0, TimeSpan.Zero);
        MessageJournalEntry expired = CreateEntry(Guid.Parse("00000000-0000-0000-0000-000000000008"), now.AddDays(-2));
        MessageJournalEntry retained = CreateEntry(Guid.Parse("00000000-0000-0000-0000-000000000009"), now);

        await store.AppendAsync(expired, cancellationToken);
        await store.AppendAsync(retained, cancellationToken);

        await using var context = new MessageJournalDbContext(options, "MessageJournal");
        Assert.Equal(
            retained.EntryId,
            Assert.Single(await context.Entries.AsNoTracking().ToListAsync(cancellationToken)).EntryId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-BOUNDS", "append-rejects-invalid-input-before-commit")]
    public async Task Append_RejectsMissingOversizedAndCanceledInputsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        DbContextOptions options = new DbContextOptionsBuilder().UseSqlite(connection).Options;
        await EnsureJournalCreatedAsync(options, cancellationToken);
        var store = new EntityFrameworkMessageJournalStore(
            options,
            "MessageJournal",
            new MessageJournalStoreLimits(1, 2, TimeSpan.FromDays(1)));
        MessageJournalEntry oversized = CreateEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000005"),
            new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "oversized"u8.ToArray());
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        ArgumentNullException missing = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await store.AppendAsync(null!, cancellationToken));
        ArgumentOutOfRangeException tooLarge = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await store.AppendAsync(oversized, cancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new EntityFrameworkMessageJournalStore(
                    options,
                    "MessageJournal",
                    new MessageJournalStoreLimits(4096, 2, TimeSpan.FromDays(1)))
                .AppendAsync(oversized, canceled.Token));

        Assert.Equal("entry", missing.ParamName);
        Assert.Equal("entry", tooLarge.ParamName);
        Assert.Equal(oversized.ContentSizeInBytes, tooLarge.ActualValue);
        await using var context = new MessageJournalDbContext(options, "MessageJournal");
        Assert.Empty(await context.Entries.AsNoTracking().ToListAsync(cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-PERSISTENCE", "duplicate-identity-rolls-back-without-replacement")]
    public async Task Append_DuplicateIdentityRollsBackWithoutReplacingTheExistingEntryAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        DbContextOptions options = new DbContextOptionsBuilder().UseSqlite(connection).Options;
        await EnsureJournalCreatedAsync(options, cancellationToken);
        var store = new EntityFrameworkMessageJournalStore(
            options,
            "MessageJournal",
            new MessageJournalStoreLimits(4096, 10, TimeSpan.FromDays(1)));
        Guid entryId = Guid.Parse("00000000-0000-0000-0000-000000000006");
        DateTimeOffset observedAt = new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        MessageJournalEntry original = CreateEntry(entryId, observedAt, "original"u8.ToArray());
        MessageJournalEntry duplicate = CreateEntry(entryId, observedAt, "replacement"u8.ToArray());

        await store.AppendAsync(original, cancellationToken);
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await store.AppendAsync(duplicate, cancellationToken));

        await using var context = new MessageJournalDbContext(options, "MessageJournal");
        MessageJournalRecord actual = Assert.Single(await context.Entries.AsNoTracking().ToListAsync(cancellationToken));
        Assert.Equal(original.Body.ToArray(), actual.Body);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-RETENTION", "sqlite-minimum-timestamp-does-not-underflow")]
    public async Task MaximumRetention_AcceptsEarliestObservationWithoutUnderflowAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        DbContextOptions options = new DbContextOptionsBuilder().UseSqlite(connection).Options;
        await EnsureJournalCreatedAsync(options, cancellationToken);
        var store = new EntityFrameworkMessageJournalStore(
            options,
            "MessageJournal",
            new MessageJournalStoreLimits(4096, 2, TimeSpan.MaxValue));
        MessageJournalEntry entry = CreateEntry(
            Guid.Parse("00000000-0000-0000-0000-000000000007"),
            DateTimeOffset.MinValue.AddDays(1));

        await store.AppendAsync(entry, cancellationToken);

        await using var context = new MessageJournalDbContext(options, "MessageJournal");
        Assert.Equal(entry.EntryId, Assert.Single(await context.Entries.AsNoTracking().ToListAsync(cancellationToken)).EntryId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-MAPPING", "bus-block-provider-selection-is-side-effect-free")]
    public void BusBlockProviderSelection_CreatesTheBoundedStoreWithoutDatabaseIo()
    {
        var limits = new MessageJournalStoreLimits(4096, 25, TimeSpan.FromDays(2));
        DbContextOptions<JournalProbeContext> options = new DbContextOptionsBuilder<JournalProbeContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var configurator = new RecordingJournalConfigurator();

        IMessageJournalConfigurator result = configurator.UseEntityFramework<JournalProbeContext>(
            options,
            "MessageJournal",
            limits,
            "journal");

        Assert.Same(configurator, result);
        var store = Assert.IsType<EntityFrameworkMessageJournalStore>(configurator.Store);
        Assert.Same(limits, store.Limits);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-CONFIGURATION", "provider-selection-rejects-every-missing-required-argument")]
    public void BusBlockProviderSelection_RejectsEveryMissingRequiredArgument()
    {
        DbContextOptions<JournalProbeContext> options = new DbContextOptionsBuilder<JournalProbeContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var limits = new MessageJournalStoreLimits(4096, 25, TimeSpan.FromDays(2));
        var configurator = new RecordingJournalConfigurator();

        ArgumentNullException missingConfigurator = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkMessageJournalConfigurationExtensions.UseEntityFramework<JournalProbeContext>(
                null!,
                options,
                "MessageJournal",
                limits));
        ArgumentNullException missingOptions = Assert.Throws<ArgumentNullException>(() =>
            configurator.UseEntityFramework<JournalProbeContext>(
                null!,
                "MessageJournal",
                limits));
        ArgumentNullException missingLimits = Assert.Throws<ArgumentNullException>(() =>
            configurator.UseEntityFramework(
                options,
                "MessageJournal",
                null!));

        Assert.Equal("configurator", missingConfigurator.ParamName);
        Assert.Equal("contextOptions", missingOptions.ParamName);
        Assert.Equal("storeLimits", missingLimits.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-CONFIGURATION", "direct-connection-rejects-every-missing-required-argument")]
    public void DirectConnection_RejectsEveryMissingRequiredArgument()
    {
        DbContextOptions options = new DbContextOptionsBuilder()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var limits = new MessageJournalStoreLimits(4096, 25, TimeSpan.FromDays(2));
        var policy = new ExcludingPolicy();
        MessageJournalOptions journalOptions = MessageJournalOptions.ContinueMessageFlow(
            TimeSpan.FromSeconds(1),
            TimeProvider.System);

        ArgumentNullException missingConfigurator = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkMessageJournalConfigurationExtensions.UseEntityFrameworkCoreMessageJournal(
                null!,
                options,
                "MessageJournal",
                policy,
                limits,
                journalOptions));
        ArgumentNullException missingOptions = Assert.Throws<ArgumentNullException>(() =>
            global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.UseEntityFrameworkCoreMessageJournal(
                    null!,
                    "MessageJournal",
                    policy,
                    limits,
                    journalOptions)));
        ArgumentNullException missingPolicy = Assert.Throws<ArgumentNullException>(() =>
            global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.UseEntityFrameworkCoreMessageJournal(
                    options,
                    "MessageJournal",
                    null!,
                    limits,
                    journalOptions)));
        ArgumentNullException missingLimits = Assert.Throws<ArgumentNullException>(() =>
            global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.UseEntityFrameworkCoreMessageJournal(
                    options,
                    "MessageJournal",
                    policy,
                    null!,
                    journalOptions)));
        ArgumentNullException missingJournalOptions = Assert.Throws<ArgumentNullException>(() =>
            global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.UseEntityFrameworkCoreMessageJournal(
                    options,
                    "MessageJournal",
                    policy,
                    limits,
                    null!)));

        Assert.Equal("configurator", missingConfigurator.ParamName);
        Assert.Equal("contextOptions", missingOptions.ParamName);
        Assert.Equal("policy", missingPolicy.ParamName);
        Assert.Equal("storeLimits", missingLimits.ParamName);
        Assert.Equal("journalOptions", missingJournalOptions.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("Journal\nTable")]
    [InlineData("Journal\0Table")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-CONFIGURATION", "relational-identifiers-reject-unsafe-or-oversized-values")]
    public void RelationalIdentifiers_RejectUnsafeOrOversizedValues(string invalidIdentifier)
    {
        DbContextOptions<JournalProbeContext> options = new DbContextOptionsBuilder<JournalProbeContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        DbContextOptions<MessageJournalDbContext> journalContextOptions =
            new DbContextOptionsBuilder<MessageJournalDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;
        var limits = new MessageJournalStoreLimits(4096, 25, TimeSpan.FromDays(2));
        var configurator = new RecordingJournalConfigurator();
        var policy = new ExcludingPolicy();
        MessageJournalOptions journalOptions = MessageJournalOptions.ContinueMessageFlow(
            TimeSpan.FromSeconds(1),
            TimeProvider.System);

        ArgumentException extensionTable = Assert.ThrowsAny<ArgumentException>(() =>
            configurator.UseEntityFramework(options, invalidIdentifier, limits));
        ArgumentException extensionSchema = Assert.ThrowsAny<ArgumentException>(() =>
            configurator.UseEntityFramework(options, "MessageJournal", limits, invalidIdentifier));
        ArgumentException directTable = Assert.ThrowsAny<ArgumentException>(() =>
            global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(bus =>
                bus.UseEntityFrameworkCoreMessageJournal(
                    options,
                    invalidIdentifier,
                    policy,
                    limits,
                    journalOptions)));
        ArgumentException directSchema = Assert.ThrowsAny<ArgumentException>(() =>
            global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(bus =>
                bus.UseEntityFrameworkCoreMessageJournal(
                    options,
                    "MessageJournal",
                    policy,
                    limits,
                    journalOptions,
                    invalidIdentifier)));
        ArgumentException storeTable = Assert.ThrowsAny<ArgumentException>(() =>
            new EntityFrameworkMessageJournalStore(options, invalidIdentifier, limits));
        ArgumentException storeSchema = Assert.ThrowsAny<ArgumentException>(() =>
            new EntityFrameworkMessageJournalStore(options, "MessageJournal", limits, invalidIdentifier));
        ArgumentException contextTable = Assert.ThrowsAny<ArgumentException>(() =>
            new MessageJournalDbContext(journalContextOptions, invalidIdentifier));
        ArgumentException contextSchema = Assert.ThrowsAny<ArgumentException>(() =>
            new MessageJournalDbContext(journalContextOptions, "MessageJournal", invalidIdentifier));
        ArgumentException mappingTable = Assert.ThrowsAny<ArgumentException>(() =>
            new MessageJournalMapping(invalidIdentifier));
        ArgumentException mappingSchema = Assert.ThrowsAny<ArgumentException>(() =>
            new MessageJournalMapping("MessageJournal", invalidIdentifier));

        Assert.Equal("tableName", extensionTable.ParamName);
        Assert.Equal("schemaName", extensionSchema.ParamName);
        Assert.Equal("tableName", directTable.ParamName);
        Assert.Equal("schemaName", directSchema.ParamName);
        Assert.Equal("tableName", storeTable.ParamName);
        Assert.Equal("schemaName", storeSchema.ParamName);
        Assert.Equal("tableName", contextTable.ParamName);
        Assert.Equal("schemaName", contextSchema.ParamName);
        Assert.Equal("tableName", mappingTable.ParamName);
        Assert.Equal("schemaName", mappingSchema.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-MAPPING", "mapping-boundaries-reject-null-builders")]
    public void MappingBoundaries_RejectNullBuilders()
    {
        var mapping = new MessageJournalMapping("MessageJournal");
        var cacheKeyFactory = new MessageJournalModelCacheKeyFactory();

        ArgumentNullException reliableModel = Assert.Throws<ArgumentNullException>(() =>
            EntityFrameworkReliableMessagingModelExtensions.AddViciOneReliableMessaging(null!));
        ArgumentNullException journalMapping = Assert.Throws<ArgumentNullException>(() =>
            mapping.Configure(null!));
        ArgumentNullException cacheContext = Assert.Throws<ArgumentNullException>(() =>
            cacheKeyFactory.Create(null!, designTime: false));
        ArgumentNullException conversionBuilder = Assert.Throws<ArgumentNullException>(() =>
            ValueConversionExtensions.HasJsonConversion<string>(null!));

        Assert.Equal("modelBuilder", reliableModel.ParamName);
        Assert.Equal("builder", journalMapping.ParamName);
        Assert.Equal("context", cacheContext.ParamName);
        Assert.Equal("builder", conversionBuilder.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("Reliable\nSchema")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-MODEL", "reliable-schema-rejects-unsafe-or-oversized-values")]
    public void ReliableMessagingModel_RejectsUnsafeOrOversizedSchema(string invalidSchema)
    {
        var builder = new ModelBuilder();

        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() =>
            builder.AddViciOneReliableMessaging(invalidSchema));

        Assert.Equal("schema", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-MAPPING", "record-serialization-is-runtime-ready")]
    public void RecordConversion_SerializesTheSanitizedCollectionsWithoutAmbientJsonConfiguration()
    {
        MessageJournalEntry entry = MessageJournalEntryTestFactory.Create(
            Guid.Parse("018cc251-f400-7000-8000-000000000020"),
            new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
            messageTypes: ["urn:message:first", "urn:message:second"],
            metadata: new Dictionary<string, string> { ["correlationId"] = "abc" },
            headers: new Dictionary<string, string> { ["tenant"] = "north" });

        MessageJournalRecord record = MessageJournalRecord.FromEntry(entry);

        Assert.Equal(2, JsonDocument.Parse(record.MessageTypesJson).RootElement.GetArrayLength());
        Assert.Equal("abc", JsonDocument.Parse(record.MetadataJson).RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("north", JsonDocument.Parse(record.HeadersJson).RootElement.GetProperty("tenant").GetString());
    }

    sealed class JournalProbeContext(DbContextOptions<JournalProbeContext> options) : DbContext(options);

    sealed class ExcludingPolicy : IMessageJournalPolicy
    {
        public ValueTask<MessageJournalProjection?> ProjectAsync(
            MessageJournalCapture capture,
            CancellationToken cancellationToken) => ValueTask.FromResult<MessageJournalProjection?>(null);
    }

    sealed class RecordingJournalConfigurator : IMessageJournalConfigurator
    {
        public IMessageJournalStore? Store { get; private set; }

        public IMessageJournalConfigurator UseStore(IMessageJournalStore store)
        {
            Store = store;
            return this;
        }

        public IMessageJournalConfigurator Policy(IMessageJournalPolicy policy) => this;

        public IMessageJournalConfigurator Options(MessageJournalOptions options) => this;
    }

    private static MessageJournalEntry CreateEntry(Guid id, DateTimeOffset observedAt, byte[]? body = null) =>
        MessageJournalEntryTestFactory.Create(id, observedAt, body: body ?? "redacted"u8.ToArray());

    private static async Task EnsureJournalCreatedAsync(DbContextOptions options, CancellationToken cancellationToken)
    {
        await using var context = new MessageJournalDbContext(options, "MessageJournal");
        Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));
    }

    private static DbContextOptions CreateOptions(string provider)
    {
        var builder = new DbContextOptionsBuilder();
        return provider switch
        {
            "sql-server" => builder.UseSqlServer(
                "Server=localhost;Database=not-opened;User Id=unused;Password=unused;Encrypt=False").Options,
            "postgresql" => builder.UseNpgsql(
                "Host=localhost;Database=not_opened;Username=unused;Password=unused").Options,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
        };
    }
}
