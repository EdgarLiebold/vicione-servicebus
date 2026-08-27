namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.EntityFrameworkCoreIntegration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

public sealed class EntityFrameworkExecutionStrategyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "successful-attempt-preserves-tracked-state")]
    public async Task SuccessfulAttempt_PreservesTrackedState()
    {
        await using var dbContext = CreateDbContext();
        var strategy = new RetryOnceExecutionStrategy(dbContext);
        var marker = new RetryMarker { Id = 1 };
        dbContext.Markers.Add(marker);
        using var cancellationTokenSource = new CancellationTokenSource();

        string result = await EntityFrameworkExecutionStrategy.ExecuteAsync(
            dbContext,
            strategy,
            () => Task.FromResult("completed"),
            cancellationTokenSource.Token);

        Assert.Equal("completed", result);
        Assert.Same(marker, Assert.Single(dbContext.ChangeTracker.Entries<RetryMarker>()).Entity);
        Assert.Equal(EntityState.Added, dbContext.Entry(marker).State);
        Assert.Equal(cancellationTokenSource.Token, strategy.RecordedCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "generic-retry-starts-with-an-empty-change-tracker")]
    public async Task GenericRetry_DiscardsTrackedStateFromTheFailedAttempt()
    {
        await using var dbContext = CreateDbContext();
        var strategy = new RetryOnceExecutionStrategy(dbContext);
        var attempts = 0;
        using var cancellationTokenSource = new CancellationTokenSource();

        string result = await EntityFrameworkExecutionStrategy.ExecuteAsync(dbContext, strategy, () =>
        {
            attempts++;
            if (attempts == 1)
            {
                dbContext.Markers.Add(new RetryMarker { Id = 1 });
                throw new RetryRequestedException();
            }

            Assert.Empty(dbContext.ChangeTracker.Entries());
            return Task.FromResult("completed");
        }, cancellationTokenSource.Token);

        Assert.Equal("completed", result);
        Assert.Equal(2, attempts);
        Assert.Equal(cancellationTokenSource.Token, strategy.RecordedCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "non-generic-retry-starts-with-an-empty-change-tracker")]
    public async Task NonGenericRetry_DiscardsTrackedStateFromTheFailedAttempt()
    {
        await using var dbContext = CreateDbContext();
        var strategy = new RetryOnceExecutionStrategy(dbContext);
        var attempts = 0;
        using var cancellationTokenSource = new CancellationTokenSource();

        await EntityFrameworkExecutionStrategy.ExecuteAsync(dbContext, strategy, () =>
        {
            attempts++;
            if (attempts == 1)
            {
                dbContext.Markers.Add(new RetryMarker { Id = 1 });
                throw new RetryRequestedException();
            }

            Assert.Empty(dbContext.ChangeTracker.Entries());
            return Task.CompletedTask;
        }, cancellationTokenSource.Token);

        Assert.Equal(2, attempts);
        Assert.Equal(cancellationTokenSource.Token, strategy.RecordedCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "failed-outbox-rollback-blocks-business-retry")]
    public async Task FailedOutboxRollback_BlocksASecondBusinessAttemptAndPreservesTheOriginalFailure()
    {
        await using var dbContext = CreateDbContext();
        var strategy = new RetryEveryExceptionExecutionStrategy(dbContext);
        var operationFailure = new RetryRequestedException();
        var cleanupFailure = new InvalidOperationException("The outbox rollback failed.");
        ConsumeContext<RetryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new RetryMessage(),
            TestContext.Current.CancellationToken);
        var outboxContext = new RollbackFailingOutboxContext<RetryMessage>(consumeContext, cleanupFailure);
        var attempts = 0;

        Exception actual = await Assert.ThrowsAsync<RetryRequestedException>(() =>
            EntityFrameworkExecutionStrategy.ExecuteAsync(
                dbContext,
                strategy,
                outboxContext,
                () =>
                {
                    attempts++;
                    return Task.FromException(operationFailure);
                }));

        Assert.Same(operationFailure, actual);
        Assert.Equal(1, attempts);
        Assert.Equal(1, outboxContext.RollbackCount);
        Assert.Equal(2, strategy.ExecutionCount);
        Assert.Same(cleanupFailure, outboxContext.CleanupFailure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "failed-outbox-rollback-blocks-canceled-business-retry")]
    public async Task FailedOutboxRollback_AfterCallerCancellationDoesNotReenterTheBusinessAttempt()
    {
        await using var dbContext = CreateDbContext();
        var strategy = new RetryEveryExceptionExecutionStrategy(dbContext);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var operationFailure = new OperationCanceledException(cancellationSource.Token);
        var cleanupFailure = new InvalidOperationException("The canceled attempt could not clean up its outbox.");
        ConsumeContext<RetryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(new RetryMessage(), cancellationSource.Token);
        var outboxContext = new RollbackFailingOutboxContext<RetryMessage>(consumeContext, cleanupFailure);
        var attempts = 0;

        Exception actual = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            EntityFrameworkExecutionStrategy.ExecuteAsync(
                dbContext,
                strategy,
                outboxContext,
                () =>
                {
                    attempts++;
                    return Task.FromException(operationFailure);
                }));

        Assert.Same(operationFailure, actual);
        Assert.Equal(cancellationSource.Token, ((OperationCanceledException)actual).CancellationToken);
        Assert.Equal(1, attempts);
        Assert.Equal(1, outboxContext.RollbackCount);
        Assert.Equal(2, strategy.ExecutionCount);
    }

    private static RetryDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<RetryDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options);

    private sealed class RetryDbContext(DbContextOptions<RetryDbContext> options) : DbContext(options)
    {
        public DbSet<RetryMarker> Markers => Set<RetryMarker>();
    }

    private sealed class RetryMarker
    {
        public int Id { get; set; }
    }

    private sealed class RetryRequestedException : Exception
    {
    }

    public sealed record RetryMessage;

    private sealed class RollbackFailingOutboxContext<T>(ConsumeContext<T> context, Exception cleanupFailure)
        : InMemoryOutboxConsumeContext<T>(context)
        where T : class
    {
        public Exception CleanupFailure { get; } = cleanupFailure;

        public int RollbackCount { get; private set; }

        public override Task DiscardPendingActions(OutboxCheckpoint checkpoint)
        {
            RollbackCount++;
            return Task.FromException(CleanupFailure);
        }
    }

    private sealed class RetryOnceExecutionStrategy(DbContext dbContext) : IExecutionStrategy
    {
        public bool RetriesOnFailure => true;

        public CancellationToken RecordedCancellationToken { get; private set; }

        public TResult Execute<TState, TResult>(
            TState state,
            Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded) =>
            throw new NotSupportedException("This test exercises only asynchronous execution.");

        public async Task<TResult> ExecuteAsync<TState, TResult>(
            TState state,
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
            CancellationToken cancellationToken = default)
        {
            RecordedCancellationToken = cancellationToken;

            try
            {
                return await operation(dbContext, state, cancellationToken);
            }
            catch (RetryRequestedException)
            {
                return await operation(dbContext, state, cancellationToken);
            }
        }
    }

    private sealed class RetryEveryExceptionExecutionStrategy(DbContext dbContext) : IExecutionStrategy
    {
        public int ExecutionCount { get; private set; }

        public bool RetriesOnFailure => true;

        public TResult Execute<TState, TResult>(
            TState state,
            Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded) =>
            throw new NotSupportedException("This test exercises only asynchronous execution.");

        public async Task<TResult> ExecuteAsync<TState, TResult>(
            TState state,
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ExecutionCount++;
                return await operation(dbContext, state, cancellationToken);
            }
            catch
            {
                ExecutionCount++;
                return await operation(dbContext, state, cancellationToken);
            }
        }
    }
}
