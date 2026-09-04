using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.EntityFrameworkCoreIntegration;

public sealed class EntityFrameworkExecutionStrategyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "successful-attempt-preserves-tracked-state")]
    public async Task SuccessfulAttempt_PreservesTrackedStateAsync()
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
    public async Task GenericRetry_DiscardsTrackedStateFromTheFailedAttemptAsync()
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
    public async Task NonGenericRetry_DiscardsTrackedStateFromTheFailedAttemptAsync()
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
    public async Task FailedOutboxRollback_BlocksASecondBusinessAttemptAndPreservesTheOriginalFailureAsync()
    {
        await using var dbContext = CreateDbContext();
        var strategy = new WrappingRetryEveryExceptionExecutionStrategy(dbContext);
        var operationFailure = new RetryRequestedException();
        var cleanupFailure = new InvalidOperationException("The outbox rollback failed.");
        var scheduler = DispatchProxy.Create<Advanced.IAdvancedMessageScheduler, FailingCancellationSchedulerProxy>();
        var schedulerProxy = (FailingCancellationSchedulerProxy)(object)scheduler;
        schedulerProxy.EnqueueCancellationFailure(cleanupFailure);
        ConsumeContext<RetryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(
            new RetryMessage(),
            TestContext.Current.CancellationToken,
            scheduler);
        var outboxContext = new InMemoryOutboxConsumeContext<RetryMessage>(consumeContext);
        if (!outboxContext.TryGetPayload(out MessageSchedulerContext? schedulerContext) || schedulerContext is null)
            throw new Xunit.Sdk.XunitException("Expected the outbox scheduler context payload to be available.");
        var attempts = 0;

        Exception actual = await Assert.ThrowsAsync<RetryRequestedException>(() =>
            EntityFrameworkExecutionStrategy.ExecuteAsync(
                dbContext,
                strategy,
                outboxContext,
                async () =>
                {
                    attempts++;
                    await schedulerContext.SchedulePublishAsync(
                        new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                        new RetryMessage(),
                        typeof(RetryMessage),
                        TestContext.Current.CancellationToken);
                    throw operationFailure;
                }));

        Assert.Same(operationFailure, actual);
        Assert.Equal(1, attempts);
        Assert.Equal(2, strategy.ExecutionCount);
        Assert.Same(cleanupFailure, strategy.FirstObservedFailure?.InnerException);
        Assert.Same(cleanupFailure, strategy.SecondObservedFailure?.InnerException);
        Assert.Equal(1, schedulerProxy.ScheduledCount);
        Assert.Equal(1, schedulerProxy.CancellationCount);

        await outboxContext.DiscardPendingActionsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, schedulerProxy.CancellationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "failed-outbox-rollback-blocks-canceled-business-retry")]
    public async Task FailedOutboxRollback_AfterCallerCancellationDoesNotReenterTheBusinessAttemptAsync()
    {
        await using var dbContext = CreateDbContext();
        var strategy = new WrappingRetryEveryExceptionExecutionStrategy(dbContext);
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
        Assert.Same(cleanupFailure, strategy.FirstObservedFailure?.InnerException);
        Assert.Same(cleanupFailure, strategy.SecondObservedFailure?.InnerException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-EXECUTION-STRATEGY", "consumed-rollback-stop-still-propagates-original-failure")]
    public async Task FailedOutboxRollback_CannotBeTurnedIntoSuccessByAConsumingStrategyAsync()
    {
        await using var dbContext = CreateDbContext();
        var strategy = new ConsumingFailureExecutionStrategy(dbContext);
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
        Assert.Equal(1, strategy.ExecutionCount);
        Assert.Same(cleanupFailure, strategy.ConsumedFailure?.InnerException);
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

        public override Task DiscardPendingActionsAsync(OutboxCheckpoint checkpoint, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); RollbackCount++;
            return Task.FromException(CleanupFailure);
        }
    }

    private class FailingCancellationSchedulerProxy : DispatchProxy
    {
        private readonly Queue<Exception> _cancellationFailures = [];

        public int CancellationCount { get; private set; }

        public int ScheduledCount { get; private set; }

        public void EnqueueCancellationFailure(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            _cancellationFailures.Enqueue(exception);
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);

            if (targetMethod.Name == nameof(IMessageScheduler.SchedulePublishAsync)
                && targetMethod.ReturnType == typeof(Task<ScheduledMessage>))
            {
                ScheduledCount++;
                var scheduledMessage = new ScheduledMessageHandle<object>(
                    NewId.NextGuid(),
                    (DateTimeOffset)args[0]!,
                    new Uri("loopback://localhost/scheduled"),
                    args[1]!);
                return Task.FromResult<ScheduledMessage>(scheduledMessage);
            }

            if (targetMethod.Name == nameof(IMessageScheduler.CancelScheduledSendAsync))
            {
                CancellationCount++;
                return _cancellationFailures.TryDequeue(out Exception? failure)
                    ? Task.FromException(failure)
                    : Task.CompletedTask;
            }

            throw new NotSupportedException(targetMethod.Name);
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

    private sealed class WrappingRetryEveryExceptionExecutionStrategy(DbContext dbContext) : IExecutionStrategy
    {
        public int ExecutionCount { get; private set; }

        public Exception? FirstObservedFailure { get; private set; }

        public Exception? SecondObservedFailure { get; private set; }

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
            catch (Exception firstFailure)
            {
                FirstObservedFailure = firstFailure;
                ExecutionCount++;

                try
                {
                    return await operation(dbContext, state, cancellationToken);
                }
                catch (Exception secondFailure)
                {
                    SecondObservedFailure = secondFailure;
                    throw new RetryLimitExceededException("The execution strategy exhausted its retry limit.", secondFailure);
                }
            }
        }
    }

    private sealed class ConsumingFailureExecutionStrategy(DbContext dbContext) : IExecutionStrategy
    {
        public Exception? ConsumedFailure { get; private set; }

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
            catch (Exception failure)
            {
                ConsumedFailure = failure;
                return default!;
            }
        }
    }
}
