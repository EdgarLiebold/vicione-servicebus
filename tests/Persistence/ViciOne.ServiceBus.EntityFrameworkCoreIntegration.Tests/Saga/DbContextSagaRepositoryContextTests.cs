namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Saga;

using System.Data;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class DbContextSagaRepositoryContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-INSERT-RACE", "existing-correlation-confirms-lost-insert-race")]
    public async Task Insert_ReturnsMissingOnlyWhenTheExactSagaNowExists()
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

        SagaConsumeContext<TestSaga, TestMessage>? inserted = await repository.Insert(
            new TestSaga { CorrelationId = correlationId, Value = "loser" });

        Assert.Null(inserted);
        TestSaga stored = await dbContext.Sagas.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("winner", stored.Value);
        Assert.DoesNotContain(dbContext.ChangeTracker.Entries<TestSaga>(), entry => entry.Entity.Value == "loser");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-INSERT-RACE", "unrelated-update-failure-preserves-identity")]
    public async Task Insert_PropagatesAnUnrelatedDatabaseFailureUnchanged()
    {
        var expected = new DbUpdateException("unrelated persistence failure");
        await using var dbContext = new FailingSagaDbContext(
            new DbContextOptionsBuilder<FailingSagaDbContext>().UseSqlite("Data Source=:memory:").Options,
            expected);
        using var repository = CreateRepositoryContext(dbContext, new EmptyLockStrategy());

        DbUpdateException actual = await Assert.ThrowsAsync<DbUpdateException>(() =>
            repository.Insert(new TestSaga { CorrelationId = Guid.NewGuid(), Value = "invalid" }));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-INSERT-RACE", "classification-failure-preserves-original-identity")]
    public async Task Insert_PreservesTheInsertFailureWhenConflictClassificationFails()
    {
        var expected = new DbUpdateException("original insert failure");
        await using var dbContext = new FailingSagaDbContext(
            new DbContextOptionsBuilder<FailingSagaDbContext>().UseSqlite("Data Source=:memory:").Options,
            expected);
        using var repository = CreateRepositoryContext(dbContext, new ThrowingLockStrategy());

        DbUpdateException actual = await Assert.ThrowsAsync<DbUpdateException>(() =>
            repository.Insert(new TestSaga { CorrelationId = Guid.NewGuid(), Value = "invalid" }));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-INSERT-RACE", "unrelated-entry-cannot-be-misclassified-as-race")]
    public async Task Insert_DoesNotSwallowAnUnrelatedEntryFailureWhenTheSagaIdentityExists()
    {
        await using var dbContext = new UnrelatedEntryFailureDbContext(
            new DbContextOptionsBuilder<UnrelatedEntryFailureDbContext>().UseSqlite("Data Source=:memory:").Options);
        using var repository = CreateRepositoryContext(dbContext, new ExistingLockStrategy());

        DbUpdateException actual = await Assert.ThrowsAsync<DbUpdateException>(() =>
            repository.Insert(new TestSaga { CorrelationId = Guid.NewGuid(), Value = "not-the-failed-entry" }));

        Assert.Same(dbContext.ExpectedException, actual);
    }

    [Theory]
    [InlineData(WriteOperation.Save)]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CONCURRENCY", "write-conflicts-map-to-provider-neutral-concurrency")]
    public async Task Write_MapsEfConcurrencyToTheProviderNeutralSagaFailure(WriteOperation operation)
    {
        var expected = new DbUpdateConcurrencyException("test-owned stale saga version");
        await using var dbContext = new FailingSagaDbContext(
            new DbContextOptionsBuilder<FailingSagaDbContext>().UseSqlite("Data Source=:memory:").Options,
            expected);
        using var repository = CreateRepositoryContext(dbContext, new EmptyLockStrategy());
        var saga = new TestSaga { CorrelationId = Guid.NewGuid(), Value = "stale" };
        SagaConsumeContext<TestSaga, TestMessage> sagaContext = await repository.Add(saga);

        ConcurrencyException actual = await Assert.ThrowsAsync<ConcurrencyException>(() =>
            ExecuteWrite(repository, sagaContext, operation));

        Assert.Same(expected, actual.InnerException);
        Assert.Equal(typeof(TestSaga), actual.SagaType);
        Assert.Equal(saga.CorrelationId, actual.CorrelationId);
    }

    [Theory]
    [InlineData(WriteOperation.Save)]
    [InlineData(WriteOperation.Update)]
    [InlineData(WriteOperation.Delete)]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CONCURRENCY", "other-write-failures-preserve-exact-identity")]
    public async Task Write_PreservesOtherEfFailuresUnchanged(WriteOperation operation)
    {
        var expected = new DbUpdateException("test-owned non-concurrency persistence failure");
        await using var dbContext = new FailingSagaDbContext(
            new DbContextOptionsBuilder<FailingSagaDbContext>().UseSqlite("Data Source=:memory:").Options,
            expected);
        using var repository = CreateRepositoryContext(dbContext, new EmptyLockStrategy());
        SagaConsumeContext<TestSaga, TestMessage> sagaContext = await repository.Add(
            new TestSaga { CorrelationId = Guid.NewGuid(), Value = "invalid" });

        DbUpdateException actual = await Assert.ThrowsAsync<DbUpdateException>(() =>
            ExecuteWrite(repository, sagaContext, operation));

        Assert.Same(expected, actual);
    }

    private static Task ExecuteWrite(
        DbContextSagaRepositoryContext<TestSaga, TestMessage> repository,
        SagaConsumeContext<TestSaga, TestMessage> sagaContext,
        WriteOperation operation)
    {
        return operation switch
        {
            WriteOperation.Save => repository.Save(sagaContext),
            WriteOperation.Update => repository.Update(sagaContext),
            WriteOperation.Delete => repository.Delete(sagaContext),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
    }

    private static DbContextSagaRepositoryContext<TestSaga, TestMessage> CreateRepositoryContext(
        DbContext dbContext,
        ISagaRepositoryLockStrategy<TestSaga> lockStrategy)
    {
        return new DbContextSagaRepositoryContext<TestSaga, TestMessage>(
            dbContext,
            CreateConsumeContext(),
            new SagaConsumeContextFactory<DbContext, TestSaga>(),
            lockStrategy);
    }

    private static ConsumeContext<TestMessage> CreateConsumeContext()
    {
        ConsumeContext<TestMessage> context = DispatchProxy.Create<ConsumeContext<TestMessage>, ConsumeContextProxy>();
        ((ConsumeContextProxy)(object)context).Configure(TestContext.Current.CancellationToken);
        return context;
    }

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
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<int>(exception);

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
            var unrelated = new UnrelatedEntity { Id = Guid.NewGuid() };
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
        public override Task<TestSaga?> Load(DbContext context, Guid correlationId, CancellationToken cancellationToken) =>
            context.Set<TestSaga>().SingleOrDefaultAsync(saga => saga.CorrelationId == correlationId, cancellationToken);
    }

    private sealed class EmptyLockStrategy : BaseLockStrategy;

    private sealed class ThrowingLockStrategy : BaseLockStrategy
    {
        public override Task<TestSaga?> Load(DbContext context, Guid correlationId, CancellationToken cancellationToken) =>
            Task.FromException<TestSaga?>(new InvalidOperationException("classification failed"));
    }

    private sealed class ExistingLockStrategy : BaseLockStrategy
    {
        public override Task<TestSaga?> Load(DbContext context, Guid correlationId, CancellationToken cancellationToken) =>
            Task.FromResult<TestSaga?>(new TestSaga { CorrelationId = correlationId, Value = "existing" });
    }

    private abstract class BaseLockStrategy : ISagaRepositoryLockStrategy<TestSaga>
    {
        public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        public bool IsTransactionEnabled => false;

        public IQueryable<TestSaga> ApplyQueryCustomization(IQueryable<TestSaga> query) => query;

        public virtual Task<TestSaga?> Load(DbContext context, Guid correlationId, CancellationToken cancellationToken) =>
            Task.FromResult<TestSaga?>(null);

        public Task<SagaLockContext<TestSaga>> CreateLockContext(
            DbContext context,
            ISagaQuery<TestSaga> query,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        private CancellationToken _cancellationToken;
        private ReceiveContext _receiveContext = null!;

        public void Configure(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
            ((ReceiveContextProxy)(object)_receiveContext).CancellationToken = cancellationToken;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_CancellationToken" => _cancellationToken,
                "get_ReceiveContext" => _receiveContext,
                "get_SerializerContext" => null,
                "get_Message" => new TestMessage(),
                "get_CorrelationId" => null,
                _ when targetMethod?.ReturnType == typeof(Task) => Task.CompletedTask,
                _ => targetMethod?.ReturnType.IsValueType == true
                    ? Activator.CreateInstance(targetMethod.ReturnType)
                    : null,
            };
        }
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
        public Task<ISendEndpoint> GetPublishSendEndpoint<T>() where T : class =>
            throw new NotSupportedException();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new NotSupportedException();
    }
}
