using System.Data;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class InboxCleanupServiceTests
{
    private static readonly DateTimeOffset Now = new(2046, 7, 8, 9, 10, 11, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-INBOX-CLEANUP", "constructor-rejects-invalid-dependencies")]
    public void Constructor_RejectsEveryMissingDependencyAndInvalidPersistenceOption()
    {
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        IOptions<InboxCleanupServiceOptions<CleanupDbContext>> options = Options.Create(CreateOptions());
        IOptions<EntityFrameworkOutboxOptions<CleanupDbContext>> persistenceOptions = Options.Create(CreatePersistenceOptions());
        var logger = NullLogger<InboxCleanupService<CleanupDbContext>>.Instance;
        var timeProvider = new FakeTimeProvider(Now);

        Assert.Throws<ArgumentNullException>(() => new InboxCleanupService<CleanupDbContext>(
            null!, persistenceOptions, logger, provider, timeProvider));
        Assert.Throws<ArgumentNullException>(() => new InboxCleanupService<CleanupDbContext>(
            options, null!, logger, provider, timeProvider));
        Assert.Throws<ArgumentNullException>(() => new InboxCleanupService<CleanupDbContext>(
            options, persistenceOptions, null!, provider, timeProvider));
        Assert.Throws<ArgumentNullException>(() => new InboxCleanupService<CleanupDbContext>(
            options, persistenceOptions, logger, null!, timeProvider));
        Assert.Throws<ArgumentNullException>(() => new InboxCleanupService<CleanupDbContext>(
            options, persistenceOptions, logger, provider, null!));

        Assert.Throws<ArgumentException>(() => new InboxCleanupService<CleanupDbContext>(
            options,
            Options.Create(new EntityFrameworkOutboxOptions<CleanupDbContext>()),
            logger,
            provider,
            timeProvider));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InboxCleanupService<CleanupDbContext>(
            options,
            Options.Create(new EntityFrameworkOutboxOptions<CleanupDbContext>
            {
                IsolationLevel = (IsolationLevel)int.MaxValue,
                LockStatementProvider = new SqliteLockStatementProvider(),
            }),
            logger,
            provider,
            timeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-INBOX-CLEANUP", "expired-delivered-rows-are-deleted-in-bounded-order")]
    public async Task Cleanup_DeletesOnlyExpiredDeliveredRowsUpToTheBatchLimitAsync()
    {
        await using CleanupEnvironment environment = await CleanupEnvironment.CreateAsync();
        Guid oldest = await environment.SeedAsync(Now - TimeSpan.FromMinutes(12));
        Guid expired = await environment.SeedAsync(Now - TimeSpan.FromMinutes(11));
        Guid nextBatch = await environment.SeedAsync(Now - TimeSpan.FromMinutes(10.5));
        Guid boundary = await environment.SeedAsync(Now - TimeSpan.FromMinutes(10));
        Guid current = await environment.SeedAsync(Now - TimeSpan.FromMinutes(5));
        Guid undelivered = await environment.SeedAsync(null);
        InboxCleanupService<CleanupDbContext> service = environment.CreateService();

        int removed = await service.CleanUpInboxStateAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, removed);
        Guid[] remaining = await environment.ReadMessageIdsAsync();
        Assert.DoesNotContain(oldest, remaining);
        Assert.DoesNotContain(expired, remaining);
        Assert.Contains(nextBatch, remaining);
        Assert.Contains(boundary, remaining);
        Assert.Contains(current, remaining);
        Assert.Contains(undelivered, remaining);
        Assert.Equal(1, await service.CleanUpInboxStateAsync(TestContext.Current.CancellationToken));
        Assert.Equal([boundary, current, undelivered], await environment.ReadMessageIdsAsync());
        Assert.Equal(0, await service.CleanUpInboxStateAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-INBOX-CLEANUP", "unavailable-ownership-lock-preserves-rows")]
    public async Task Cleanup_WhenOwnershipLockIsUnavailablePreservesEveryRowAsync()
    {
        await using CleanupEnvironment environment = await CleanupEnvironment.CreateAsync("SELECT 0");
        Guid expired = await environment.SeedAsync(Now - TimeSpan.FromMinutes(12));
        InboxCleanupService<CleanupDbContext> service = environment.CreateService();

        int removed = await service.CleanUpInboxStateAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, removed);
        Assert.Contains(expired, await environment.ReadMessageIdsAsync());
    }

    [Theory]
    [InlineData("SELECT NULL")]
    [InlineData("SELECT 1 WHERE 0")]
    [InlineData("SELECT 2")]
    [RequirementCoverage("REQ-VSB-EF-INBOX-CLEANUP", "nonowner-lock-results-preserve-rows")]
    public async Task Cleanup_WhenLockResultDoesNotGrantOwnershipPreservesEveryRowAsync(string lockStatement)
    {
        await using CleanupEnvironment environment = await CleanupEnvironment.CreateAsync(lockStatement);
        Guid expired = await environment.SeedAsync(Now - TimeSpan.FromMinutes(12));
        InboxCleanupService<CleanupDbContext> service = environment.CreateService();

        int removed = await service.CleanUpInboxStateAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, removed);
        Assert.Equal([expired], await environment.ReadMessageIdsAsync());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-INBOX-CLEANUP", "rollback-failure-does-not-mask-primary-failure")]
    public async Task RollbackTransaction_RejectsMissingTransactionAndSuppressesSecondaryFailureAsync()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            InboxCleanupService<CleanupDbContext>.RollbackTransactionAsync(null!));
        IDbContextTransaction successfulTransaction = DispatchProxy.Create<IDbContextTransaction, TrackingRollbackTransactionProxy>();
        var successfulProxy = (TrackingRollbackTransactionProxy)(object)successfulTransaction;

        await InboxCleanupService<CleanupDbContext>.RollbackTransactionAsync(successfulTransaction);

        Assert.Equal(1, successfulProxy.RollbackCalls);
        IDbContextTransaction failingTransaction = DispatchProxy.Create<IDbContextTransaction, TrackingRollbackTransactionProxy>();
        var failingProxy = (TrackingRollbackTransactionProxy)(object)failingTransaction;
        failingProxy.ShouldFail = true;

        await InboxCleanupService<CleanupDbContext>.RollbackTransactionAsync(failingTransaction);

        Assert.Equal(1, failingProxy.RollbackCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-INBOX-CLEANUP", "caller-cancellation-remains-observable")]
    public async Task Cleanup_CallerCancellationRemainsObservableAsync()
    {
        await using CleanupEnvironment environment = await CleanupEnvironment.CreateAsync();
        InboxCleanupService<CleanupDbContext> service = environment.CreateService();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CleanUpInboxStateAsync(cancellation.Token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-INBOX-CLEANUP", "hosted-loop-removes-work-and-stops")]
    public async Task HostedLoop_RemovesAvailableWorkAndStopsOnHostCancellationAsync()
    {
        await using CleanupEnvironment environment = await CleanupEnvironment.CreateAsync();
        await environment.SeedAsync(TimeProvider.System.GetUtcNow() - TimeSpan.FromHours(1));
        InboxCleanupServiceOptions<CleanupDbContext> options = CreateOptions();
        options.QueryDelay = TimeSpan.FromMilliseconds(5);
        InboxCleanupService<CleanupDbContext> service = environment.CreateService(options, TimeProvider.System);

        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await AssertEventuallyAsync(async () => await environment.CountAsync() == 0);
        }
        finally
        {
            await service.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task AssertEventuallyAsync(Func<Task<bool>> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        while (!await condition())
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
    }

    private static InboxCleanupServiceOptions<CleanupDbContext> CreateOptions() => new()
    {
        DuplicateDetectionWindow = TimeSpan.FromMinutes(10),
        QueryMessageLimit = 2,
        QueryTimeout = TimeSpan.FromSeconds(5),
        QueryDelay = TimeSpan.FromSeconds(1),
    };

    private static EntityFrameworkOutboxOptions<CleanupDbContext> CreatePersistenceOptions(
        ILockStatementProvider? provider = null) => new()
        {
            IsolationLevel = IsolationLevel.Serializable,
            LockStatementProvider = provider ?? new SqliteLockStatementProvider(),
        };

    private sealed class CleanupDbContext(DbContextOptions<CleanupDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddInboxStateEntity();
    }

    private sealed class StatementLockProvider(string statement) : ILockStatementProvider
    {
        public string GetRowLockStatement<T>(DbContext context)
            where T : class => throw new NotSupportedException();

        public string GetRowLockStatement<T>(DbContext context, params string[] propertyNames)
            where T : class => throw new NotSupportedException();

        public string GetOutboxStatement(DbContext context) => throw new NotSupportedException();

        public string GetInboxCleanupLockStatement(DbContext context) => statement;
    }

    private class TrackingRollbackTransactionProxy : DispatchProxy
    {
        public int RollbackCalls { get; private set; }

        public bool ShouldFail { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IDbContextTransaction.RollbackAsync))
            {
                RollbackCalls++;
                return ShouldFail
                    ? Task.FromException(new InvalidOperationException("Secondary rollback failure."))
                    : Task.CompletedTask;
            }

            throw new InvalidOperationException($"Unexpected transaction member: {targetMethod?.Name ?? "<null>"}.");
        }
    }

    private sealed class CleanupEnvironment : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<CleanupDbContext> _dbContextOptions;
        private readonly ILockStatementProvider _lockStatementProvider;
        private readonly ServiceProvider _provider;

        private CleanupEnvironment(
            SqliteConnection connection,
            DbContextOptions<CleanupDbContext> dbContextOptions,
            ILockStatementProvider lockStatementProvider,
            ServiceProvider provider)
        {
            _connection = connection;
            _dbContextOptions = dbContextOptions;
            _lockStatementProvider = lockStatementProvider;
            _provider = provider;
        }

        public static async Task<CleanupEnvironment> CreateAsync(string? lockStatement = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            DbContextOptions<CleanupDbContext> dbContextOptions =
                new DbContextOptionsBuilder<CleanupDbContext>().UseSqlite(connection).Options;
            await using (var setupContext = new CleanupDbContext(dbContextOptions))
                await setupContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            ServiceProvider provider = new ServiceCollection()
                .AddScoped(_ => new CleanupDbContext(dbContextOptions))
                .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            ILockStatementProvider lockProvider = lockStatement == null
                ? new SqliteLockStatementProvider()
                : new StatementLockProvider(lockStatement);
            return new CleanupEnvironment(connection, dbContextOptions, lockProvider, provider);
        }

        public InboxCleanupService<CleanupDbContext> CreateService(
            InboxCleanupServiceOptions<CleanupDbContext>? options = null,
            TimeProvider? timeProvider = null) => new(
            Options.Create(options ?? CreateOptions()),
            Options.Create(CreatePersistenceOptions(_lockStatementProvider)),
            NullLogger<InboxCleanupService<CleanupDbContext>>.Instance,
            _provider,
            timeProvider ?? new FakeTimeProvider(Now));

        public async Task<Guid> SeedAsync(DateTimeOffset? delivered)
        {
            var state = new InboxState
            {
                MessageId = Guid.NewGuid(),
                ConsumerId = Guid.NewGuid(),
                LockId = Guid.NewGuid(),
                Received = Now,
                Delivered = delivered,
            };
            await using var dbContext = new CleanupDbContext(_dbContextOptions);
            dbContext.Add(state);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            return state.MessageId;
        }

        public async Task<Guid[]> ReadMessageIdsAsync()
        {
            await using var dbContext = new CleanupDbContext(_dbContextOptions);
            return await dbContext.Set<InboxState>()
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => x.MessageId)
                .ToArrayAsync(TestContext.Current.CancellationToken);
        }

        public async Task<int> CountAsync()
        {
            await using var dbContext = new CleanupDbContext(_dbContextOptions);
            return await dbContext.Set<InboxState>().CountAsync(TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
