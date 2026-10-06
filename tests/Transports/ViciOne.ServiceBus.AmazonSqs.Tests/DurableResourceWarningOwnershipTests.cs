using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class DurableResourceWarningOwnershipTests
{
    static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-warning-cannot-abandon-pending-owned-release")]
    public async Task WarningFromCancellation_DoesNotFinishRemovalOrDisposalBeforeHeldOwnershipAsync(bool disposeStore, bool hostileLogger)
    {
        ILogContext? previous = LogContext.Current;
        var callbackFailure = new IOException("Controlled durable owner cancellation callback failure");
        var diagnosticFailure = new ApplicationException("Controlled durable cancellation warning failure");
        var logger = new WarningLogger(hostileLogger, diagnosticFailure);
        var store = new DurableResourceStore<string, TrackedResource>(CancellationToken.None);
        var lateResource = new TrackedResource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int callbackCalls = 0;
        Task<TrackedResource>? creation = null;
        Task? operation = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            creation = store.GetOrAddAsync("held", async (_, ownerToken) =>
            {
                using CancellationTokenRegistration registration = ownerToken.Register(() =>
                {
                    Interlocked.Increment(ref callbackCalls);
                    canceled.TrySetResult();
                    throw callbackFailure;
                });
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
                return lateResource;
            }, CancellationToken.None);
            await started.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

            operation = disposeStore ? store.DisposeAsync().AsTask() : store.RemoveAsync("held");
            await canceled.Task.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            bool operationCompletedWhileFactoryHeld = operation.IsCompleted;
            Assert.False(release.Task.IsCompleted);
            Assert.False(creation.IsCompleted);
            Assert.Equal(1, Volatile.Read(ref callbackCalls));
            Assert.Equal(1, logger.WarningCalls);
            Exception actualCancellationFailure = Assert.Single(logger.Exceptions);
            Assert.True(ContainsCause(actualCancellationFailure, callbackFailure));

            release.TrySetResult();
            Exception? observed = await Record.ExceptionAsync(async () =>
                await operation.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                creation.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, lateResource.DisposalCount);
            Assert.False(operationCompletedWhileFactoryHeld);
            Assert.Null(observed);
            if (!disposeStore)
            {
                Assert.True(await ((Task<bool>)operation).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
                Assert.False(store.TryGet("held", out _));
            }
        }
        finally
        {
            logger.DisableFailure();
            release.TrySetResult();
            try { await ObserveAsync(operation); }
            finally
            {
                try { await ObserveAsync(creation); }
                finally
                {
                    try { await ObserveAsync(store.DisposeAsync().AsTask()); }
                    finally { LogContext.Current = previous; }
                }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "durable-warning-cannot-skip-remaining-resource-disposal")]
    public async Task WarningFromDisposal_DoesNotSkipTheRemainingCommittedResourceAsync(bool hostileLogger)
    {
        ILogContext? previous = LogContext.Current;
        var disposalFailure = new IOException("Controlled durable resource disposal failure");
        var diagnosticFailure = new ApplicationException("Controlled durable disposal warning failure");
        var logger = new WarningLogger(hostileLogger, diagnosticFailure);
        var store = new DurableResourceStore<string, TrackedResource>(CancellationToken.None);
        var faulting = new TrackedResource(disposalFailure);
        var healthy = new TrackedResource();
        Task? operation = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            Assert.Same(faulting, await store.GetOrAddAsync("first", (_, _) => ValueTask.FromResult(faulting),
                TestContext.Current.CancellationToken));
            Assert.Same(healthy, await store.GetOrAddAsync("second", (_, _) => ValueTask.FromResult(healthy),
                TestContext.Current.CancellationToken));

            operation = store.DisposeAsync().AsTask();
            Exception? observed = await Record.ExceptionAsync(async () =>
                await operation.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, logger.WarningCalls);
            Assert.Same(disposalFailure, Assert.Single(logger.Exceptions));
            Assert.Equal(1, faulting.DisposalCount);
            Assert.Equal(1, healthy.DisposalCount);
            Assert.Null(observed);
        }
        finally
        {
            logger.DisableFailure();
            try { await ObserveAsync(operation); }
            finally
            {
                try { await ObserveAsync(store.DisposeAsync().AsTask()); }
                finally
                {
                    try
                    {
                        // A failed original disposal can skip this managed probe value. This
                        // fallback runs only after observations and never supplies assertion credit.
                        if (healthy.DisposalCount == 0)
                            await healthy.DisposeAsync();
                    }
                    finally { LogContext.Current = previous; }
                }
            }
        }
    }

    static bool ContainsCause(Exception actual, Exception expected)
    {
        if (ReferenceEquals(actual, expected))
            return true;
        if (actual is AggregateException aggregate)
            return aggregate.InnerExceptions.Any(cause => ContainsCause(cause, expected));
        return actual.InnerException is not null && ContainsCause(actual.InnerException, expected);
    }

    static async Task ObserveAsync(Task? task)
    {
        if (task is null)
            return;
        try
        {
            await task.WaitAsync(OperationTimeout, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not TimeoutException)
        {
            // Observe the actual operation's fault after releasing its controlled gate.
        }
    }

    sealed class TrackedResource(Exception? failure = null) : IAsyncDisposable
    {
        int _disposalCount;
        public int DisposalCount => Volatile.Read(ref _disposalCount);
        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposalCount);
            return failure is null ? ValueTask.CompletedTask : ValueTask.FromException(failure);
        }
    }

    sealed class WarningLogger(bool hostile, Exception failure) : ILogger
    {
        int _throw = hostile ? 1 : 0;
        int _warningCalls;
        public int WarningCalls => Volatile.Read(ref _warningCalls);
        public ConcurrentQueue<Exception> Exceptions { get; } = new();
        public void DisableFailure() => Interlocked.Exchange(ref _throw, 0);
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new EmptyScope();

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel != LogLevel.Warning || exception is null ||
                !formatter(state, exception).StartsWith("Durable resource ", StringComparison.Ordinal))
                return;
            Interlocked.Increment(ref _warningCalls);
            Exceptions.Enqueue(exception);
            if (Volatile.Read(ref _throw) != 0)
                throw failure;
        }

        sealed class EmptyScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
