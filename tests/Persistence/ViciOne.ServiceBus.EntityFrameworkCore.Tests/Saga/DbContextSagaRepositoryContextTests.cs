using System.Data;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Saga;

public sealed class DbContextSagaRepositoryContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-INSERT-RACE", "existing-correlation-confirms-lost-insert-race")]
    public async Task Insert_ReturnsMissingOnlyWhenTheExactSagaNowExistsAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        DbContextOptions<SagaDbContext> options = new DbContextOptionsBuilder<SagaDbContext>()
            .UseSqlite(connection)
            .Options;
        Guid correlationId = Guid.Parse("d2f9f9bc-b516-45fb-83ea-7fb2ad8aa48b");

        await using var dbContext = new SagaDbContext(options);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        dbContext.Sagas.Add(new TestSaga { CorrelationId = correlationId, Value = "winner" });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        dbContext.ChangeTracker.Clear();

        using var repository = CreateRepositoryContext(dbContext, new QueryingLockStrategy());

        SagaConsumeContext<TestSaga, TestMessage>? inserted = await repository.InsertAsync(new TestSaga { CorrelationId = correlationId, Value = "loser" }, TestContext.Current.CancellationToken);

        Assert.Null(inserted);
        TestSaga stored = await dbContext.Sagas.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("winner", stored.Value);
        Assert.DoesNotContain(dbContext.ChangeTracker.Entries<TestSaga>(), entry => entry.Entity.Value == "loser");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-INSERT-RACE", "unrelated-update-failure-preserves-identity")]
    public async Task Insert_PropagatesAnUnrelatedDatabaseFailureUnchangedAsync()
    {
        var expected = new DbUpdateException("unrelated persistence failure");
        await using var dbContext = new FailingSagaDbContext(
            new DbContextOptionsBuilder<FailingSagaDbContext>().UseSqlite("Data Source=:memory:").Options,
            expected);
        using var repository = CreateRepositoryContext(dbContext, new EmptyLockStrategy());

        DbUpdateException actual = await Assert.ThrowsAsync<DbUpdateException>(() =>
            repository.InsertAsync(new TestSaga { CorrelationId = Guid.NewGuid(), Value = "invalid" }, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-INSERT-RACE", "classification-failure-preserves-original-identity")]
    public async Task Insert_PreservesTheInsertFailureWhenConflictClassificationFailsAsync()
    {
        var expected = new DbUpdateException("original insert failure");
        await using var dbContext = new FailingSagaDbContext(
            new DbContextOptionsBuilder<FailingSagaDbContext>().UseSqlite("Data Source=:memory:").Options,
            expected);
        using var repository = CreateRepositoryContext(dbContext, new ThrowingLockStrategy());

        DbUpdateException actual = await Assert.ThrowsAsync<DbUpdateException>(() =>
            repository.InsertAsync(new TestSaga { CorrelationId = Guid.NewGuid(), Value = "invalid" }, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-INSERT-RACE", "unrelated-entry-cannot-be-misclassified-as-race")]
    public async Task Insert_DoesNotSwallowAnUnrelatedEntryFailureWhenTheSagaIdentityExistsAsync()
    {
        await using var dbContext = new UnrelatedEntryFailureDbContext(
            new DbContextOptionsBuilder<UnrelatedEntryFailureDbContext>().UseSqlite("Data Source=:memory:").Options);
        using var repository = CreateRepositoryContext(dbContext, new ExistingLockStrategy());

        DbUpdateException actual = await Assert.ThrowsAsync<DbUpdateException>(() =>
            repository.InsertAsync(new TestSaga { CorrelationId = Guid.NewGuid(), Value = "not-the-failed-entry" }, TestContext.Current.CancellationToken));

        Assert.Same(dbContext.ExpectedException, actual);
    }

    [Theory]
    [InlineData(WriteOperation.Save)]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CONCURRENCY", "write-conflicts-map-to-provider-neutral-concurrency")]
    public async Task Write_MapsEfConcurrencyToTheProviderNeutralSagaFailureAsync(WriteOperation operation)
    {
        var expected = new DbUpdateConcurrencyException("test-owned stale saga version");
        await using var dbContext = new FailingSagaDbContext(
            new DbContextOptionsBuilder<FailingSagaDbContext>().UseSqlite("Data Source=:memory:").Options,
            expected);
        using var repository = CreateRepositoryContext(dbContext, new EmptyLockStrategy());
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Value = "stale" };
        SagaConsumeContext<TestSaga, TestMessage> sagaContext = await repository.AddAsync(saga, TestContext.Current.CancellationToken);

        ConcurrencyException actual = await Assert.ThrowsAsync<ConcurrencyException>(() =>
            ExecuteWriteAsync(repository, sagaContext, operation));

        Assert.Same(expected, actual.InnerException);
        Assert.Equal(typeof(TestSaga), actual.SagaType);
        Assert.Equal(saga.CorrelationId, actual.CorrelationId);
    }

    [Theory]
    [InlineData(WriteOperation.Save)]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CONCURRENCY", "other-write-failures-preserve-exact-identity")]
    public async Task Write_PreservesOtherEfFailuresUnchangedAsync(WriteOperation operation)
    {
        var expected = new DbUpdateException("test-owned non-concurrency persistence failure");
        await using var dbContext = new FailingSagaDbContext(
            new DbContextOptionsBuilder<FailingSagaDbContext>().UseSqlite("Data Source=:memory:").Options,
            expected);
        using var repository = CreateRepositoryContext(dbContext, new EmptyLockStrategy());
        SagaConsumeContext<TestSaga, TestMessage> sagaContext = await repository.AddAsync(new TestSaga { CorrelationId = Guid.NewGuid(), Value = "invalid" }, TestContext.Current.CancellationToken);

        DbUpdateException actual = await Assert.ThrowsAsync<DbUpdateException>(() =>
            ExecuteWriteAsync(repository, sagaContext, operation));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CANCELLATION", "explicit-operation-token-overrides-ambient-token")]
    public async Task Load_UsesTheExplicitOperationTokenAheadOfTheAmbientTokenAsync()
    {
        using var ambientCancellation = new CancellationTokenSource();
        ambientCancellation.Cancel();
        await using var dbContext = new SagaDbContext(
            new DbContextOptionsBuilder<SagaDbContext>().UseSqlite("Data Source=:memory:").Options);
        var lockStrategy = new RecordingLockStrategy();
        using var repository = CreateRepositoryContext(dbContext, lockStrategy, ambientCancellation.Token);

        SagaConsumeContext<TestSaga, TestMessage>? result = await repository.LoadAsync(
            Guid.Parse("b0c0115c-42f1-49c8-a5bf-bd08f16120ad"),
            TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Equal(1, lockStrategy.LoadCount);
        Assert.Equal(TestContext.Current.CancellationToken, lockStrategy.LastCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CANCELLATION", "default-operation-token-inherits-ambient-cancellation")]
    public async Task Load_InheritsAmbientCancellationWhenNoOperationTokenIsSuppliedAsync()
    {
        using var ambientCancellation = new CancellationTokenSource();
        ambientCancellation.Cancel();
        await using var dbContext = new SagaDbContext(
            new DbContextOptionsBuilder<SagaDbContext>().UseSqlite("Data Source=:memory:").Options);
        var lockStrategy = new RecordingLockStrategy();
        using var repository = CreateRepositoryContext(dbContext, lockStrategy, ambientCancellation.Token);

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            LoadWithoutOperationTokenAsync(repository, Guid.Parse("f5bb594b-c5dd-469e-8e86-f46470458708")));

        Assert.Equal(ambientCancellation.Token, exception.CancellationToken);
        Assert.Equal(0, lockStrategy.LoadCount);
    }

    private static Task<SagaConsumeContext<TestSaga, TestMessage>?> LoadWithoutOperationTokenAsync(
        DbContextSagaRepositoryContext<TestSaga, TestMessage> repository,
        Guid correlationId) => repository.LoadAsync(correlationId);

    private static Task ExecuteWriteAsync(
        DbContextSagaRepositoryContext<TestSaga, TestMessage> repository,
        SagaConsumeContext<TestSaga, TestMessage> sagaContext,
        WriteOperation operation)
    {
        return operation switch
        {
            WriteOperation.Save => repository.SaveAsync(sagaContext),
            WriteOperation.Update => repository.UpdateAsync(sagaContext),
            WriteOperation.Delete => repository.DeleteAsync(sagaContext),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
    }

    private static DbContextSagaRepositoryContext<TestSaga, TestMessage> CreateRepositoryContext(
        DbContext dbContext,
        ISagaRepositoryLockStrategy<TestSaga> lockStrategy,
        CancellationToken? consumeCancellationToken = null)
    {
        return new DbContextSagaRepositoryContext<TestSaga, TestMessage>(
            dbContext,
            CreateConsumeContext(consumeCancellationToken ?? TestContext.Current.CancellationToken),
            new SagaConsumeContextFactory<DbContext, TestSaga>(),
            lockStrategy);
    }

    private static ConsumeContext<TestMessage> CreateConsumeContext(CancellationToken cancellationToken)
    {
        TestConsumeContext context = DispatchProxy.Create<TestConsumeContext, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(cancellationToken);
        return context;
    }

    private interface TestConsumeContext : ConsumeContext<TestMessage>, ConsumeContext;

    public sealed class TestSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public sealed record TestMessage;

    public enum WriteOperation
    {
        Save,
        Update,
        Delete,
    }

    private sealed class SagaDbContext(DbContextOptions<SagaDbContext> options) : DbContext(options)
    {
        public DbSet<TestSaga> Sagas => Set<TestSaga>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestSaga>().HasKey(saga => saga.CorrelationId);
        }
    }

    private sealed class FailingSagaDbContext(DbContextOptions<FailingSagaDbContext> options, DbUpdateException exception) : DbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<int>(cancellationToken); return Task.FromException<int>(exception); }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestSaga>().HasKey(saga => saga.CorrelationId);
        }
    }

    private sealed class UnrelatedEntryFailureDbContext(DbContextOptions<UnrelatedEntryFailureDbContext> options) : DbContext(options)
    {
        public DbUpdateException? ExpectedException { get; private set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<int>(cancellationToken); var unrelated = new UnrelatedEntity { Id = Guid.NewGuid() };
            EntityEntry unrelatedEntry = Entry(unrelated);
            unrelatedEntry.State = EntityState.Added;
            ExpectedException = new DbUpdateException("unrelated entry failure", [unrelatedEntry]);

            return Task.FromException<int>(ExpectedException);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestSaga>().HasKey(saga => saga.CorrelationId);
            modelBuilder.Entity<UnrelatedEntity>().HasKey(entity => entity.Id);
        }
    }

    private sealed class UnrelatedEntity
    {
        public Guid Id { get; init; }
    }

    private sealed class QueryingLockStrategy : BaseLockStrategy
    {
        public override Task<TestSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken = default) =>
            context.Set<TestSaga>().SingleOrDefaultAsync(saga => saga.CorrelationId == correlationId, cancellationToken);
    }

    private sealed class EmptyLockStrategy : BaseLockStrategy;

    private sealed class ThrowingLockStrategy : BaseLockStrategy
    {
        public override Task<TestSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.EntityFrameworkCore.Tests.Saga.DbContextSagaRepositoryContextTests.TestSaga?>(cancellationToken); return Task.FromException<TestSaga?>(new InvalidOperationException("classification failed")); }
    }

    private sealed class ExistingLockStrategy : BaseLockStrategy
    {
        public override Task<TestSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.EntityFrameworkCore.Tests.Saga.DbContextSagaRepositoryContextTests.TestSaga?>(cancellationToken); return Task.FromResult<TestSaga?>(new TestSaga { CorrelationId = correlationId, Value = "existing" }); }
    }

    private sealed class RecordingLockStrategy : BaseLockStrategy
    {
        public int LoadCount { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public override Task<TestSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken = default)
        {
            LoadCount++;
            LastCancellationToken = cancellationToken;
            return Task.FromResult<TestSaga?>(null);
        }
    }

    private abstract class BaseLockStrategy : ISagaRepositoryLockStrategy<TestSaga>
    {
        public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        public bool IsTransactionEnabled => false;

        public IQueryable<TestSaga> ApplyQueryCustomization(IQueryable<TestSaga> query) => query;

        public virtual Task<TestSaga?> LoadAsync(DbContext context, Guid correlationId, CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.EntityFrameworkCore.Tests.Saga.DbContextSagaRepositoryContextTests.TestSaga?>(cancellationToken); return Task.FromResult<TestSaga?>(null); }
        public Task<SagaLockContext<TestSaga>> CreateLockContextAsync(
            DbContext context,
            ISagaQuery<TestSaga> query,
            CancellationToken cancellationToken)
        { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.EntityFrameworkCore.Saga.SagaLockContext<global::ViciOne.ServiceBus.EntityFrameworkCore.Tests.Saga.DbContextSagaRepositoryContextTests.TestSaga>>(cancellationToken); throw new NotSupportedException(); }
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        private CancellationToken _cancellationToken;
        private ReceiveContext _receiveContext = null!;
        private SerializerContext _serializerContext = null!;

        public void Configure(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
            ((ReceiveContextProxy)(object)_receiveContext).CancellationToken = cancellationToken;
            _serializerContext = DispatchProxy.Create<SerializerContext, SerializerContextProxy>();
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_CancellationToken" => _cancellationToken,
                "get_ReceiveContext" => _receiveContext,
                "get_SerializerContext" => _serializerContext,
                "get_Message" => new TestMessage(),
                "get_CorrelationId" => null,
                _ when targetMethod?.ReturnType == typeof(Task) => Task.CompletedTask,
                _ => targetMethod?.ReturnType.IsValueType == true
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null,
            };
        }
    }

    private class SerializerContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected serializer-context member: {targetMethod?.Name ?? "<null>"}.");
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        private static readonly IPublishEndpointProvider PublishEndpointProvider = new NoopPublishEndpointProvider();

        public CancellationToken CancellationToken { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_CancellationToken" => CancellationToken,
                "get_PublishEndpointProvider" => PublishEndpointProvider,
                _ when targetMethod?.ReturnType == typeof(Task) => Task.CompletedTask,
                _ => targetMethod?.ReturnType.IsValueType == true
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null,
            };
        }
    }

    private sealed class NoopPublishEndpointProvider : IPublishEndpointProvider
    {
        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default) where T : class { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); throw new NotSupportedException(); }
        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException();
    }
}
