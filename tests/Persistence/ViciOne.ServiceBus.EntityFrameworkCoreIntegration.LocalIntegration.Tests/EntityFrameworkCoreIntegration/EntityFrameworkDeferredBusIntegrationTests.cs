using System.Collections.Concurrent;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transactions;
using ViciOne.ServiceBus.Transactions;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

public sealed class EntityFrameworkDeferredBusIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DEFERRED-BUS", "ambient-rollback-discards-database-and-publish")]
    public async Task AmbientRollback_DiscardsTheDatabaseWriteAndBufferedPublish()
    {
        await using DeferredBusFixture fixture = await DeferredBusFixture.StartAsync("deferred-bus-rollback");
        var message = new TransactionalMessage(NewId.NextGuid(), "rollback");

        using (fixture.CreateTransactionScope())
        {
            await fixture.Insert(message, TestContext.Current.CancellationToken);
            await fixture.EnlistedBus.Publish(message, TestContext.Current.CancellationToken);

            Assert.Empty(fixture.PublishObserver.Events);
            Assert.False(fixture.Received.IsCompleted);
        }

        Assert.Empty(fixture.PublishObserver.Events);
        Assert.False(fixture.Received.IsCompleted);
        Assert.False(await fixture.Exists(message.CorrelationId, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DEFERRED-BUS", "ambient-commit-persists-and-publishes-exactly-once")]
    public async Task AmbientCommit_PersistsTheDatabaseWriteAndPublishesExactlyOnce()
    {
        await using DeferredBusFixture fixture = await DeferredBusFixture.StartAsync("deferred-bus-commit");
        var message = new TransactionalMessage(NewId.NextGuid(), "commit");

        using (TransactionScope transaction = fixture.CreateTransactionScope())
        {
            await fixture.Insert(message, TestContext.Current.CancellationToken);
            await fixture.EnlistedBus.Publish(message, TestContext.Current.CancellationToken);

            Assert.Empty(fixture.PublishObserver.Events);
            Assert.False(fixture.Received.IsCompleted);
            transaction.Complete();
        }

        TransactionalMessage received = await fixture.Received.WaitAsync(
            fixture.OperationTimeout,
            TestContext.Current.CancellationToken);

        Assert.Equal(message, received);
        Assert.Equal(["Pre", "Post"], fixture.PublishObserver.Events.Select(item => item.Stage));
        Assert.All(fixture.PublishObserver.Events, item => Assert.Same(message, item.Message));
        Assert.True(await fixture.Exists(message.CorrelationId, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DEFERRED-BUS", "explicit-flush-publishes-buffer-once")]
    public async Task ExplicitFlush_PublishesTheBufferedMessageExactlyOnce()
    {
        await using DeferredBusFixture fixture = await DeferredBusFixture.StartAsync("deferred-bus-flush");
        var message = new TransactionalMessage(NewId.NextGuid(), "release");
        IBufferedBus bufferedBus = new BufferedBusTestDriver(fixture.Harness.Bus).Bus;

        await fixture.Insert(message, TestContext.Current.CancellationToken);
        await bufferedBus.Publish(message, TestContext.Current.CancellationToken);

        Assert.Empty(fixture.PublishObserver.Events);
        Assert.False(fixture.Received.IsCompleted);

        await bufferedBus.FlushAsync(TestContext.Current.CancellationToken)
            .WaitAsync(fixture.OperationTimeout, TestContext.Current.CancellationToken);
        TransactionalMessage received = await fixture.Received.WaitAsync(
            fixture.OperationTimeout,
            TestContext.Current.CancellationToken);
        await bufferedBus.FlushAsync(TestContext.Current.CancellationToken)
            .WaitAsync(fixture.OperationTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(message, received);
        Assert.Equal(["Pre", "Post"], fixture.PublishObserver.Events.Select(item => item.Stage));
        Assert.All(fixture.PublishObserver.Events, item => Assert.Same(message, item.Message));
        Assert.True(await fixture.Exists(message.CorrelationId, TestContext.Current.CancellationToken));
    }

    public sealed record TransactionalMessage(Guid CorrelationId, string Value);

    private sealed class TransactionalRecord
    {
        public Guid CorrelationId { get; init; }
        public required string Value { get; init; }
    }

    private sealed class TransactionalDbContext(DbContextOptions<TransactionalDbContext> options) : DbContext(options)
    {
        public DbSet<TransactionalRecord> Records => Set<TransactionalRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TransactionalRecord>(entity =>
            {
                entity.ToTable("TransactionalRecords");
                entity.HasKey(record => record.CorrelationId);
                entity.Property(record => record.Value).HasMaxLength(128);
            });
        }
    }

    private sealed class DeferredBusFixture : IAsyncDisposable
    {
        private readonly PostgreSqlTestDatabase _database;
        private readonly DbContextOptions<TransactionalDbContext> _dbContextOptions;
        private readonly TaskCompletionSource<TransactionalMessage> _received;
        private readonly ConnectHandle _publishObserverHandle;

        private DeferredBusFixture(
            PostgreSqlTestDatabase database,
            DbContextOptions<TransactionalDbContext> dbContextOptions,
            InMemoryTestHarness harness,
            RecordingPublishObserver publishObserver,
            ConnectHandle publishObserverHandle,
            TaskCompletionSource<TransactionalMessage> received)
        {
            _database = database;
            _dbContextOptions = dbContextOptions;
            Harness = harness;
            PublishObserver = publishObserver;
            _publishObserverHandle = publishObserverHandle;
            _received = received;
            EnlistedBus = new AmbientTransactionBusTestDriver(harness.Bus).Bus;
        }

        public IAmbientTransactionBus EnlistedBus { get; }
        public InMemoryTestHarness Harness { get; }
        public TimeSpan OperationTimeout => _database.OperationTimeout;
        public RecordingPublishObserver PublishObserver { get; }
        public Task<TransactionalMessage> Received => _received.Task;

        public static async Task<DeferredBusFixture> StartAsync(string purpose)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(purpose, cancellationToken);
            DbContextOptions<TransactionalDbContext> options = new DbContextOptionsBuilder<TransactionalDbContext>()
                .UseNpgsql(database.ConnectionString)
                .Options;
            await using (var context = new TransactionalDbContext(options))
                await context.Database.EnsureCreatedAsync(cancellationToken);

            var harness = new InMemoryTestHarness($"{purpose}-{NewId.NextGuid():N}")
            {
                TestTimeout = database.OperationTimeout,
                TestInactivityTimeout = database.OperationTimeout,
            };
            TaskCompletionSource<TransactionalMessage> received = new(TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Handler<TransactionalMessage>(context =>
            {
                received.TrySetResult(context.Message);
                return Task.CompletedTask;
            });

            try
            {
                await harness.Start(cancellationToken);
                var observer = new RecordingPublishObserver();
                ConnectHandle handle = harness.Bus.ConnectPublishObserver(observer);
                return new DeferredBusFixture(database, options, harness, observer, handle, received);
            }
            catch
            {
                await harness.Stop();
                harness.Dispose();
                await database.DisposeAsync();
                throw;
            }
        }

        public TransactionScope CreateTransactionScope()
        {
            var options = new TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                Timeout = OperationTimeout,
            };
            return new TransactionScope(
                TransactionScopeOption.RequiresNew,
                options,
                TransactionScopeAsyncFlowOption.Enabled);
        }

        public async Task Insert(TransactionalMessage message, CancellationToken cancellationToken)
        {
            await using var context = new TransactionalDbContext(_dbContextOptions);
            context.Records.Add(new TransactionalRecord
            {
                CorrelationId = message.CorrelationId,
                Value = message.Value,
            });
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> Exists(Guid correlationId, CancellationToken cancellationToken)
        {
            await using var context = new TransactionalDbContext(_dbContextOptions);
            return await context.Records.AsNoTracking()
                .AnyAsync(record => record.CorrelationId == correlationId, cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            _publishObserverHandle.Dispose();
            await Harness.Stop().WaitAsync(OperationTimeout, CancellationToken.None);
            Harness.Dispose();
            await _database.DisposeAsync();
        }

    }

    private sealed class RecordingPublishObserver : IPublishObserver
    {
        private readonly ConcurrentQueue<PublishObservation> _events = new();

        public PublishObservation[] Events => _events.ToArray();

        public Task PrePublish<T>(PublishContext<T> context)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Pre", context.Message));
            return Task.CompletedTask;
        }

        public Task PostPublish<T>(PublishContext<T> context)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Post", context.Message));
            return Task.CompletedTask;
        }

        public Task PublishFault<T>(PublishContext<T> context, Exception exception)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Fault", context.Message));
            return Task.CompletedTask;
        }
    }

    private sealed record PublishObservation(string Stage, object Message);
}
