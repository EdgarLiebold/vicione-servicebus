namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.DurableSend;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class EntityFrameworkDurableSendStoreTests
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-MODEL", "bounded-record-capacity-and-claim-indexes")]
    public void Model_MapsBoundedRecordsCapacityLedgerAndClaimIndexes()
    {
        var options = new DbContextOptionsBuilder<DurableDbContext>()
            .UseSqlServer("Server=localhost;Database=not-opened;User Id=unused;Password=unused;Encrypt=False")
            .Options;
        using var context = new DurableDbContext(options, schema: "reliability");
        IEntityType record = context.Model.FindEntityType(typeof(DurableSendRecord))!;
        IEntityType capacity = context.Model.FindEntityType(typeof(DurableSendCapacityState))!;

        Assert.Equal("DurableSend", record.GetTableName());
        Assert.Equal("reliability", record.GetSchema());
        Assert.Equal([nameof(DurableSendRecord.StoreKey), nameof(DurableSendRecord.Id)],
            record.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(128, record.FindProperty(nameof(DurableSendRecord.StoreKey))!.GetMaxLength());
        Assert.Equal(320, record.FindProperty(nameof(DurableSendRecord.ContractIdentity))!.GetMaxLength());
        Assert.Equal(SerializedDurableSend.MaximumDestinationAddressCharacters,
            record.FindProperty(nameof(DurableSendRecord.DestinationAddress))!.GetMaxLength());
        Assert.Equal(SerializedDurableSend.MaximumContentTypeCharacters,
            record.FindProperty(nameof(DurableSendRecord.ContentType))!.GetMaxLength());
        Assert.Equal(512, record.FindProperty(nameof(DurableSendRecord.LastFailureType))!.GetMaxLength());
        Assert.Contains(record.GetIndexes(), index => index.Properties.Select(property => property.Name)
            .SequenceEqual([
                nameof(DurableSendRecord.StoreKey),
                nameof(DurableSendRecord.Status),
                nameof(DurableSendRecord.NextAttemptAt),
                nameof(DurableSendRecord.EnqueuedAt),
            ]));
        Assert.Contains(record.GetIndexes(), index => index.Properties.Select(property => property.Name)
            .SequenceEqual([nameof(DurableSendRecord.StoreKey), nameof(DurableSendRecord.LeaseExpiresAt)]));
        Assert.Equal("DurableSendCapacity", capacity.GetTableName());
        Assert.Equal(nameof(DurableSendCapacityState.StoreKey), Assert.Single(capacity.FindPrimaryKey()!.Properties).Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-STORE", "real-sqlite-lifecycle-and-generation-fencing")]
    public async Task Store_PersistsTheCompleteLifecycleAndFencesAReusedIdentityGeneration()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.Create(cancellationToken);
        var validator = new RecordingValidator();
        IDurableSendStore<ITestBus> store = database.CreateStore<ITestBus>("lifecycle", validator);
        SerializedDurableSend message = Message(1, body: [1, 2, 3], metadata: [4, 5]);
        var limits = new DurableSendStoreLimits(1, 5);

        DurableSendAdmissionResult accepted = await store.AdmitAsync(message, limits, Epoch, cancellationToken);
        DurableSendAdmissionResult duplicate = await store.AdmitAsync(message, limits, Epoch.AddHours(1), cancellationToken);
        Assert.Equal(DurableSendAdmissionDisposition.Accepted, accepted.Disposition);
        Assert.Equal(DurableSendAdmissionDisposition.AlreadyAccepted, duplicate.Disposition);
        Assert.Equal((1, 5L), (duplicate.StoredCount, duplicate.StoredBytes));
        await Assert.ThrowsAsync<DurableSendIdentityConflictException>(() =>
            store.AdmitAsync(message with { Metadata = new byte[] { 9, 9 } }, limits, Epoch, cancellationToken));
        await Assert.ThrowsAsync<DurableSendCapacityExceededException>(() =>
            store.AdmitAsync(Message(2, body: [], metadata: []), limits, Epoch, cancellationToken));

        DurableSendDelivery first = Assert.Single(await store.ClaimDueAsync(
            Epoch,
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        Assert.True(await store.ScheduleRetryAsync(
            message.Id,
            first.Lease,
            1,
            Epoch.AddMinutes(2),
            DurableSendFailureKind.Transient,
            "Tests.Transient",
            Epoch,
            cancellationToken));
        Assert.Empty(await store.ClaimDueAsync(
            Epoch.AddMinutes(2).AddTicks(-1),
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));

        DurableSendDelivery retry = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddMinutes(2),
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        Assert.Equal(1, retry.DeliveryAttempts);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.QuarantineAsync(
            message.Id,
            first.Lease,
            2,
            DurableSendFailureKind.NonRetryable,
            "Tests.StaleLease",
            Epoch.AddMinutes(2),
            cancellationToken));
        Assert.True(await store.AwaitConsumerCompletionAsync(
            message.Id,
            retry.Lease,
            2,
            Epoch.AddMinutes(7),
            cancellationToken));
        Assert.Equal(1, (await store.GetSnapshotAsync(cancellationToken)).AwaitingConsumerCompletionCount);
        Assert.True(await store.CompleteConsumerDeliveryAsync(
            message.Id,
            retry.GenerationToken,
            Epoch.AddMinutes(3),
            cancellationToken));
        Assert.Equal(0, (await store.GetSnapshotAsync(cancellationToken)).StoredCount);

        await store.AdmitAsync(message, limits, Epoch.AddMinutes(4), cancellationToken);
        DurableSendDelivery incarnation = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddMinutes(4),
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        Assert.NotEqual(retry.GenerationToken, incarnation.GenerationToken);
        Assert.False(await store.CompleteConsumerDeliveryAsync(
            message.Id,
            retry.GenerationToken,
            Epoch.AddMinutes(4),
            cancellationToken));
        Assert.True(await store.QuarantineAsync(
            message.Id,
            incarnation.Lease,
            1,
            DurableSendFailureKind.NonRetryable,
            "Tests.Permanent",
            Epoch.AddMinutes(4),
            cancellationToken));
        DurableSendQuarantineEntry evidence = Assert.Single(await store.GetQuarantineAsync(1, cancellationToken));
        Assert.Equal(message.Id, evidence.Id);
        Assert.Equal(DurableSendFailureKind.NonRetryable, evidence.FailureKind);
        Assert.Equal(DurableSendAdmissionDisposition.AlreadyQuarantined,
            (await store.AdmitAsync(message, limits, Epoch, cancellationToken)).Disposition);

        Assert.True(await store.RequeueAsync(message.Id, Epoch.AddMinutes(5), cancellationToken));
        DurableSendDelivery requeued = Assert.Single(await store.ClaimDueAsync(
            Epoch.AddMinutes(5),
            1,
            TimeSpan.FromMinutes(1),
            cancellationToken));
        Assert.Equal(0, requeued.DeliveryAttempts);
        Assert.True(await store.MarkDeliveredAsync(message.Id, requeued.Lease, Epoch.AddMinutes(5), cancellationToken));
        DurableSendStoreSnapshot empty = await store.GetSnapshotAsync(cancellationToken);
        Assert.Equal((0, 0L, 0, 0),
            (empty.StoredCount, empty.StoredBytes, empty.PendingCount, empty.QuarantinedCount));
        Assert.Equal(1, validator.CallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-CAPACITY", "concurrent-conditional-ledger-admission")]
    public async Task Store_ConcurrentAdmissionsCannotOvershootTheHardLedger()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.Create(cancellationToken);
        IDurableSendStore<ITestBus> store = database.CreateStore<ITestBus>("concurrent", new RecordingValidator());
        var limits = new DurableSendStoreLimits(5, 20);

        Task<bool>[] attempts = Enumerable.Range(1, 20).Select(async id =>
        {
            try
            {
                await store.AdmitAsync(Message(id), limits, Epoch, cancellationToken);
                return true;
            }
            catch (DurableSendCapacityExceededException)
            {
                return false;
            }
        }).ToArray();

        bool[] results = await Task.WhenAll(attempts);
        DurableSendStoreSnapshot snapshot = await store.GetSnapshotAsync(cancellationToken);
        Assert.Equal(5, results.Count(static accepted => accepted));
        Assert.Equal((5, 5L), (snapshot.StoredCount, snapshot.StoredBytes));

        IDurableSendStore<ITestBus> byteStore = database.CreateStore<ITestBus>(
            "concurrent-bytes",
            new RecordingValidator());
        var byteLimits = new DurableSendStoreLimits(20, 5);
        Task<bool>[] byteAttempts = Enumerable.Range(101, 20).Select(async id =>
        {
            try
            {
                await byteStore.AdmitAsync(Message(id), byteLimits, Epoch, cancellationToken);
                return true;
            }
            catch (DurableSendCapacityExceededException)
            {
                return false;
            }
        }).ToArray();

        bool[] byteResults = await Task.WhenAll(byteAttempts);
        DurableSendStoreSnapshot byteSnapshot = await byteStore.GetSnapshotAsync(cancellationToken);
        Assert.Equal(5, byteResults.Count(static accepted => accepted));
        Assert.Equal((5, 5L), (byteSnapshot.StoredCount, byteSnapshot.StoredBytes));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-RECOVERY", "missing-ledger-rebuilt-by-server-aggregate")]
    public async Task Store_ReconstructsAMissingCapacityLedgerFromRetainedRows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DurableDatabase database = await DurableDatabase.Create(cancellationToken);
        await using (DurableDbContext seed = database.Factory.CreateDbContext())
        {
            seed.AddRange(
                Record("recovery", 1, storageSize: 3),
                Record("recovery", 2, storageSize: 7));
            await seed.SaveChangesAsync(cancellationToken);
        }
        IDurableSendStore<ITestBus> store = database.CreateStore<ITestBus>("recovery", new RecordingValidator());

        DurableSendStoreSnapshot snapshot = await store.GetSnapshotAsync(cancellationToken);

        Assert.Equal((2, 10L, 2), (snapshot.StoredCount, snapshot.StoredBytes, snapshot.PendingCount));
        await using DurableDbContext verify = database.Factory.CreateDbContext();
        DurableSendCapacityState ledger = await verify.Set<DurableSendCapacityState>()
            .SingleAsync(item => item.StoreKey == "recovery", cancellationToken);
        Assert.Equal((2, 10L), (ledger.StoredCount, ledger.StoredBytes));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-PREFLIGHT", "custom-validator-runs-before-store-query")]
    public async Task Store_ExecutesTheProviderDurabilityPreflightBeforeInitializationQueries()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new ExpectedPreflightException();
        var validator = new ThrowingValidator(expected);
        var factory = new CountingFactory(new DbContextOptionsBuilder<DurableDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);
        var store = new EntityFrameworkDurableSendStore<ITestBus, DurableDbContext>(
            factory,
            Options.Create(new EntityFrameworkDurableSendStoreOptions<ITestBus> { StoreKey = "preflight" }),
            validator);

        ExpectedPreflightException actual = await Assert.ThrowsAsync<ExpectedPreflightException>(() =>
            store.GetSnapshotAsync(cancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, validator.CallCount);
        Assert.Equal(1, factory.CreateCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-COMPOSITION", "stable-store-key-custom-validator-and-single-owner")]
    public void Registration_PreservesProviderValidatorAndRejectsAmbiguousStoreOwnership()
    {
        var validator = new RecordingValidator();
        var services = new ServiceCollection();
        services.AddSingleton<IEntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>>(validator);

        Assert.Same(services, services.AddEntityFrameworkDurableSendStore<ITestBus, DurableDbContext>(
            options => options.StoreKey = "orders"));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IEntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>)
            && ReferenceEquals(descriptor.ImplementationInstance, validator));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IDurableSendStore<ITestBus>));
        Assert.Throws<ConfigurationException>(() => services.AddEntityFrameworkDurableSendStore<ITestBus, DurableDbContext>(
            options => options.StoreKey = "other"));

        var missing = Options.Create(new EntityFrameworkDurableSendStoreOptions<ITestBus>());
        var factory = new CountingFactory(new DbContextOptionsBuilder<DurableDbContext>().Options);
        Assert.Throws<ConfigurationException>(() =>
            new EntityFrameworkDurableSendStore<ITestBus, DurableDbContext>(factory, missing, validator));
        Assert.Throws<ConfigurationException>(() => new EntityFrameworkDurableSendStore<ITestBus, DurableDbContext>(
            factory,
            Options.Create(new EntityFrameworkDurableSendStoreOptions<ITestBus> { StoreKey = new string('x', 129) }),
            validator));
        Assert.Throws<ConfigurationException>(() => new EntityFrameworkDurableSendStore<ITestBus, DurableDbContext>(
            factory,
            Options.Create(new EntityFrameworkDurableSendStoreOptions<ITestBus> { StoreKey = "bad\nkey" }),
            validator));

        var defaultStore = new EntityFrameworkDurableSendStore<IBus, DurableDbContext>(
            factory,
            Options.Create(new EntityFrameworkDurableSendStoreOptions<IBus>()),
            new RecordingValidator<IBus>());
        Assert.NotNull(defaultStore);
    }

    private static SerializedDurableSend Message(int id, byte[]? body = null, byte[]? metadata = null) => new()
    {
        Id = new DurableSendId(GuidFrom(id)),
        ContractIdentity = new MessageContractIdentity("vicione.tests.ef-durable", 1),
        DestinationAddress = new Uri("loopback://ef-durable"),
        ContentType = "application/octet-stream",
        Body = body ?? new byte[] { 1 },
        Metadata = metadata ?? [],
    };

    private static DurableSendRecord Record(string storeKey, int id, long storageSize) => new()
    {
        StoreKey = storeKey,
        Id = GuidFrom(id),
        GenerationToken = Guid.NewGuid(),
        ContractIdentity = new MessageContractIdentity("vicione.tests.ef-recovery", 1).ToString(),
        DestinationAddress = "loopback://ef-recovery/",
        ContentType = "application/octet-stream",
        Body = new byte[checked((int)storageSize)],
        StorageSize = storageSize,
        Status = DurableSendStatus.Pending,
        EnqueuedAt = Epoch.UtcDateTime,
    };

    private static Guid GuidFrom(int value) => new(value, 0, 0, new byte[8]);

    private interface ITestBus : IBus;

    private sealed class DurableDbContext(DbContextOptions<DurableDbContext> options, string? schema = null)
        : DbContext(options)
    {
        private readonly string? _schema = schema;

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddViciOneDurableSender(_schema);
    }

    private sealed class DurableDbContextFactory(DbContextOptions<DurableDbContext> options)
        : IDbContextFactory<DurableDbContext>
    {
        public DurableDbContext CreateDbContext() => new(options);
    }

    private sealed class CountingFactory(DbContextOptions<DurableDbContext> options)
        : IDbContextFactory<DurableDbContext>
    {
        public int CreateCount { get; private set; }

        public DurableDbContext CreateDbContext()
        {
            CreateCount++;
            return new DurableDbContext(options);
        }
    }

    private class RecordingValidator<TBus> : IEntityFrameworkDurableSendCommitDurabilityValidator<TBus>
        where TBus : class, IBus
    {
        public int CallCount { get; private set; }

        public Task ValidateAsync(DbContext dbContext, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingValidator : RecordingValidator<ITestBus>;

    private sealed class ThrowingValidator(ExpectedPreflightException exception)
        : IEntityFrameworkDurableSendCommitDurabilityValidator<ITestBus>
    {
        public int CallCount { get; private set; }

        public Task ValidateAsync(DbContext dbContext, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromException(exception);
        }
    }

    private sealed class DurableDatabase : IAsyncDisposable
    {
        private readonly string _path;

        private DurableDatabase(string path, DurableDbContextFactory factory)
        {
            _path = path;
            Factory = factory;
        }

        public DurableDbContextFactory Factory { get; }

        public static async Task<DurableDatabase> Create(CancellationToken cancellationToken)
        {
            string path = Path.Combine(Path.GetTempPath(), $"vicione-durable-store-{Guid.NewGuid():N}.db");
            var options = new DbContextOptionsBuilder<DurableDbContext>()
                .UseSqlite($"Data Source={path};Default Timeout=30;Pooling=False")
                .Options;
            var factory = new DurableDbContextFactory(options);
            await using DurableDbContext context = factory.CreateDbContext();
            await context.Database.EnsureCreatedAsync(cancellationToken);
            return new DurableDatabase(path, factory);
        }

        public IDurableSendStore<TBus> CreateStore<TBus>(
            string storeKey,
            IEntityFrameworkDurableSendCommitDurabilityValidator<TBus> validator)
            where TBus : class, IBus =>
            new EntityFrameworkDurableSendStore<TBus, DurableDbContext>(
                Factory,
                Options.Create(new EntityFrameworkDurableSendStoreOptions<TBus> { StoreKey = storeKey }),
                validator);

        public ValueTask DisposeAsync()
        {
            File.Delete(_path);
            File.Delete(_path + "-wal");
            File.Delete(_path + "-shm");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ExpectedPreflightException : Exception;
}
