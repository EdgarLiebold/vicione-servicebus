using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class TaskExecutorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-EXECUTE", "action")]
    public async Task ExecuteAction_CompletesOnlyAfterTheActionExecutedAsync()
    {
        await using var executor = new TaskExecutor();
        var executed = false;

        await executor.ExecuteAsync(() => { executed = true; }, TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.True(executed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-EXECUTE", "task")]
    public async Task ExecuteTask_CompletesOnlyAfterTheAwaitedWorkCompletedAsync()
    {
        await using var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var executed = false;

        Task run = executor.ExecuteAsync(
            (Func<Task>)(async () =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                executed = true;
            }),
            TestCancellationToken);

        try
        {
            await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            Assert.False(run.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await run.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.True(executed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-EXECUTE", "value-task")]
    public async Task ExecuteValueTask_CompletesOnlyAfterTheAwaitedWorkCompletedAsync()
    {
        await using var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var executed = false;

        Task run = executor.ExecuteValueTaskAsync(
            async () =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                executed = true;
            },
            TestCancellationToken);

        try
        {
            await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            Assert.False(run.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await run.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.True(executed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-RESULT", "sync-task-and-value-task")]
    public async Task ResultOverloads_ReturnTheExecutedDelegateResultsAsync()
    {
        await using var executor = new TaskExecutor();

        int synchronous = await executor.ExecuteAsync(() => 11, TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);
        int task = await executor.ExecuteAsync(() => Task.FromResult(12), TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);
        int valueTask = await executor.ExecuteValueTaskAsync(() => ValueTask.FromResult(13), TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal(11, synchronous);
        Assert.Equal(12, task);
        Assert.Equal(13, valueTask);
    }

    [Theory]
    [InlineData(false, 0, 1, "concurrencyLimit")]
    [InlineData(true, 0, 1, "capacity")]
    [InlineData(true, 1, 0, "concurrencyLimit")]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-CONSTRUCTION", "positive-limits")]
    public void NonPositiveLimits_AreRejected(bool bounded, int first, int concurrencyLimit, string parameterName)
    {
        ArgumentOutOfRangeException exception = bounded
            ? Assert.Throws<ArgumentOutOfRangeException>(() => new TaskExecutor(first, concurrencyLimit))
            : Assert.Throws<ArgumentOutOfRangeException>(() => new TaskExecutor(first));

        Assert.Equal(parameterName, exception.ParamName);
        Assert.Equal(0, exception.ActualValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-CONCURRENCY", "maximum-active-work")]
    public async Task ConcurrencyLimit_BoundsSimultaneouslyActiveWorkAsync()
    {
        const int concurrencyLimit = 2;
        await using var executor = new TaskExecutor(concurrencyLimit);
        var saturated = NewCompletionSource();
        var release = NewCompletionSource();
        var active = 0;
        var maximum = 0;

        Task[] work = Enumerable.Range(0, 6)
            .Select(_ => executor.ExecuteAsync((Func<Task>)(async () =>
            {
                int current = Interlocked.Increment(ref active);
                UpdateMaximum(ref maximum, current);
                if (current == concurrencyLimit)
                    saturated.TrySetResult();

                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                Interlocked.Decrement(ref active);
            }), TestCancellationToken))
            .ToArray();

        try
        {
            await saturated.Task.WaitAsync(OperationTimeout, TestCancellationToken);

            Assert.Equal(concurrencyLimit, Volatile.Read(ref maximum));
            Assert.All(work, task => Assert.False(task.IsCompleted));
        }
        finally
        {
            release.TrySetResult();
        }

        await Task.WhenAll(work).WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal(0, Volatile.Read(ref active));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-BACKPRESSURE", "bounded-capacity")]
    public async Task BoundedExecutor_BlocksTheNextWriterWhileCapacityIsOccupiedAsync()
    {
        await using var executor = new TaskExecutor(capacity: 1, concurrencyLimit: 1);
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var secondExecuted = false;
        var thirdExecuted = false;

        Task first = executor.ExecuteAsync(
            (Func<Task>)(async () =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            }),
            TestCancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        await executor.EnqueueAsync(() => { secondExecuted = true; }, TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);
        Task thirdAdmission = executor.EnqueueAsync(() => { thirdExecuted = true; }, TestCancellationToken);

        try
        {
            Assert.False(thirdAdmission.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await first.WaitAsync(OperationTimeout, TestCancellationToken);
        await thirdAdmission.WaitAsync(OperationTimeout, TestCancellationToken);

        await executor.DisposeAsync();
        Assert.True(secondExecuted);
        Assert.True(thirdExecuted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-BACKPRESSURE", "bounded-default-capacity")]
    public async Task DefaultExecutor_HasAHardBoundedAdmissionCapacityAsync()
    {
        await using var executor = new TaskExecutor(concurrencyLimit: 1);
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var executed = 0;

        Task first = executor.ExecuteAsync(
            (Func<Task>)(async () =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            }),
            TestCancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        for (var index = 0; index < 32; index++)
        {
            await executor.EnqueueAsync(() => Interlocked.Increment(ref executed), TestCancellationToken)
                .WaitAsync(OperationTimeout, TestCancellationToken);
        }

        Task overflowAdmission = executor.EnqueueAsync(() => Interlocked.Increment(ref executed), TestCancellationToken);
        try
        {
            Assert.False(overflowAdmission.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await first.WaitAsync(OperationTimeout, TestCancellationToken);
        await overflowAdmission.WaitAsync(OperationTimeout, TestCancellationToken);
        await executor.DisposeAsync();

        Assert.Equal(33, Volatile.Read(ref executed));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-CANCELLATION", "queued-work")]
    public async Task CancellationAfterEnqueue_CancelsTheResultWithoutInvokingTheDelegateAsync()
    {
        await using var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var invoked = false;

        Task first = executor.ExecuteAsync(
            (Func<Task>)(async () =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            }),
            TestCancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        using var cancellation = new CancellationTokenSource();
        Task canceled = executor.ExecuteAsync(() => invoked = true, cancellation.Token);
        cancellation.Cancel();
        release.TrySetResult();

        await first.WaitAsync(OperationTimeout, TestCancellationToken);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(canceled.IsCanceled);
        Assert.False(invoked);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-CANCELLATION", "accepted-fire-and-forget-work-owns-completion")]
    public async Task CancellationAfterQueuedAdmission_DoesNotRevokeAcceptedFireAndForgetWorkAsync()
    {
        await using var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var invoked = false;

        Task first = executor.ExecuteAsync(async () =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        }, TestCancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        using var cancellation = new CancellationTokenSource();
        await executor.EnqueueAsync(() => { invoked = true; }, cancellation.Token)
            .WaitAsync(OperationTimeout, TestCancellationToken);
        cancellation.Cancel();
        release.TrySetResult();

        await first.WaitAsync(OperationTimeout, TestCancellationToken);
        await executor.DisposeAsync();

        Assert.True(invoked);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-CANCELLATION", "bounded-admission")]
    public async Task CancellationWhileWaitingForCapacity_CancelsAdmissionWithoutInvokingTheDelegateAsync()
    {
        await using var executor = new TaskExecutor(capacity: 1, concurrencyLimit: 1);
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var invoked = false;

        Task first = executor.ExecuteAsync(
            (Func<Task>)(async () =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            }),
            TestCancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        await executor.EnqueueAsync(() => { }, TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);
        using var cancellation = new CancellationTokenSource();
        Task waiting = executor.ExecuteAsync(() => invoked = true, cancellation.Token);

        try
        {
            Assert.False(waiting.IsCompleted);
            cancellation.Cancel();

            OperationCanceledException exception =
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
            Assert.Equal(cancellation.Token, exception.CancellationToken);
            Assert.True(waiting.IsCanceled);
            Assert.False(invoked);
        }
        finally
        {
            release.TrySetResult();
        }

        await first.WaitAsync(OperationTimeout, TestCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-FAULT", "unwrapped-exception")]
    public async Task DelegateFault_IsPropagatedWithoutAnAggregateWrapperAsync()
    {
        await using var executor = new TaskExecutor();

        var exception = await Assert.ThrowsAsync<ExpectedTaskExecutorException>(() =>
            executor.ExecuteAsync(
                () => Task.FromException(new ExpectedTaskExecutorException("expected")),
                TestCancellationToken));

        Assert.Equal("expected", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-CANCELLATION", "delegate-cancellation")]
    public async Task DelegateCancellation_PreservesTheCancellationTokenAndTaskStateAsync()
    {
        await using var executor = new TaskExecutor();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task canceled = executor.ExecuteAsync(() => Task.FromCanceled(cancellation.Token), TestCancellationToken);
        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(canceled.IsCanceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-ENQUEUE", "dispose-drains-work")]
    public async Task Enqueue_ReturnsAfterEnqueueAndDisposeWaitsForTheWorkAsync()
    {
        var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var executed = false;

        Task disposal;
        try
        {
            await executor.EnqueueAsync(
                (Func<Task>)(async () =>
                {
                    started.TrySetResult();
                    await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                    executed = true;
                }),
                TestCancellationToken).WaitAsync(OperationTimeout, TestCancellationToken);
            await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

            disposal = executor.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await disposal.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.True(executed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-ENQUEUE", "value-task-dispose-drains-work")]
    public async Task EnqueueValueTask_ReturnsAfterEnqueueAndDisposeWaitsForWorkAsync()
    {
        var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var executed = false;

        Task disposal;
        try
        {
            await executor.EnqueueValueTaskAsync(
                async () =>
                {
                    started.TrySetResult();
                    await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                    executed = true;
                },
                TestCancellationToken).WaitAsync(OperationTimeout, TestCancellationToken);
            await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

            disposal = executor.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await disposal.WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.True(executed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-DISPOSAL", "concurrent-idempotent-and-closed")]
    public async Task ConcurrentDisposal_DrainsAcceptedWorkAndRejectsNewWorkAsync()
    {
        var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();

        await executor.EnqueueAsync(
            (Func<Task>)(async () =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            }),
            TestCancellationToken).WaitAsync(OperationTimeout, TestCancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        Task[] disposals = Enumerable.Range(0, 4)
            .Select(_ => executor.DisposeAsync().AsTask())
            .ToArray();
        Assert.All(disposals, disposal => Assert.False(disposal.IsCompleted));

        ObjectDisposedException whileDisposing = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            executor.ExecuteAsync(() => { }, TestCancellationToken));
        Assert.Equal(nameof(TaskExecutor), whileDisposing.ObjectName);

        release.TrySetResult();
        await Task.WhenAll(disposals).WaitAsync(OperationTimeout, TestCancellationToken);

        await executor.DisposeAsync();

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            executor.ExecuteAsync(() => { }, TestCancellationToken));
        Assert.Equal(nameof(TaskExecutor), exception.ObjectName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-FAULT", "queued-fault-and-secondary-logger")]
    public async Task QueuedFaultAndSecondaryLoggerFault_DoNotTerminateTheWorkerAsync()
    {
        ILogContext? previousLogContext = LogContext.Current;
        var logger = new ThrowingLogger();
        LogContext.ConfigureCurrentLogContext(logger);
        var executor = new TaskExecutor(capacity: 1, concurrencyLimit: 1);
        var followUpExecuted = false;

        try
        {
            await executor.EnqueueAsync(
                () => Task.FromException(new ExpectedTaskExecutorException("queued")),
                TestCancellationToken).WaitAsync(OperationTimeout, TestCancellationToken);
            Task followUp = executor.ExecuteAsync(() => { followUpExecuted = true; }, TestCancellationToken);

            await logger.Called.WaitAsync(OperationTimeout, TestCancellationToken);
            await executor.DisposeAsync();
            await followUp.WaitAsync(OperationTimeout, TestCancellationToken);
        }
        finally
        {
            try
            {
                await executor.DisposeAsync();
            }
            finally
            {
                LogContext.Current = previousLogContext;
            }
        }

        Assert.Equal(1, logger.CallCount);
        Assert.True(followUpExecuted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-BACKPRESSURE", "blocking-callback-admission")]
    public async Task EnqueueBlocking_BlocksOnlyUntilBoundedCapacityIsAvailableAsync()
    {
        await using var executor = new TaskExecutor(capacity: 1, concurrencyLimit: 1);
        var firstStarted = NewCompletionSource();
        var release = NewCompletionSource();
        var blockingCallEntered = NewCompletionSource();
        var blockingCallReturned = NewCompletionSource();
        CancellationToken cancellationToken = TestCancellationToken;

        Task first = executor.ExecuteAsync(
            (Func<Task>)(async () =>
            {
                firstStarted.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, cancellationToken);
            }),
            cancellationToken);
        await firstStarted.Task.WaitAsync(OperationTimeout, cancellationToken);
        await executor.EnqueueAsync(() => { }, cancellationToken)
            .WaitAsync(OperationTimeout, cancellationToken);

        var caller = new Thread(() =>
        {
            try
            {
                blockingCallEntered.TrySetResult();
                executor.EnqueueBlocking(() => Task.CompletedTask, cancellationToken);
                blockingCallReturned.TrySetResult();
            }
            catch (Exception exception)
            {
                blockingCallReturned.TrySetException(exception);
            }
        })
        { IsBackground = true };
        caller.Start();

        await blockingCallEntered.Task.WaitAsync(OperationTimeout, cancellationToken);
        try
        {
            Assert.False(blockingCallReturned.Task.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await blockingCallReturned.Task.WaitAsync(OperationTimeout, cancellationToken);
        await first.WaitAsync(OperationTimeout, cancellationToken);
        await executor.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-CANCELLATION", "blocking-admission")]
    public async Task EnqueueBlocking_CancelsWhileWaitingForBoundedCapacityAsync()
    {
        await using var executor = new TaskExecutor(capacity: 1, concurrencyLimit: 1);
        var firstStarted = NewCompletionSource();
        var release = NewCompletionSource();
        var blockingCallEntered = NewCompletionSource();
        var cancellationObserved = NewCompletionSource<CancellationToken>();
        CancellationToken testCancellationToken = TestCancellationToken;

        Task first = executor.ExecuteAsync(async () =>
        {
            firstStarted.TrySetResult();
            await release.Task.WaitAsync(OperationTimeout, testCancellationToken);
        }, testCancellationToken);
        await firstStarted.Task.WaitAsync(OperationTimeout, testCancellationToken);
        await executor.EnqueueAsync(() => { }, testCancellationToken)
            .WaitAsync(OperationTimeout, testCancellationToken);

        using var cancellation = new CancellationTokenSource();
        var caller = new Thread(() =>
        {
            try
            {
                blockingCallEntered.TrySetResult();
                executor.EnqueueBlocking(() => Task.CompletedTask, cancellation.Token);
                cancellationObserved.TrySetException(
                    new InvalidOperationException("Blocking admission unexpectedly succeeded."));
            }
            catch (OperationCanceledException exception)
            {
                cancellationObserved.TrySetResult(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                cancellationObserved.TrySetException(exception);
            }
        })
        { IsBackground = true };
        caller.Start();

        try
        {
            await blockingCallEntered.Task.WaitAsync(OperationTimeout, testCancellationToken);
            Assert.False(cancellationObserved.Task.IsCompleted);
            cancellation.Cancel();

            CancellationToken observed = await cancellationObserved.Task
                .WaitAsync(OperationTimeout, testCancellationToken);
            Assert.Equal(cancellation.Token, observed);
        }
        finally
        {
            cancellation.Cancel();
            release.TrySetResult();
        }

        await first.WaitAsync(OperationTimeout, testCancellationToken);
        await executor.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-DISPOSAL", "blocking-admission-closed")]
    public async Task EnqueueBlocking_RejectsWorkAfterDisposalAsync()
    {
        var executor = new TaskExecutor();
        await executor.DisposeAsync();

        ObjectDisposedException exception = Assert.Throws<ObjectDisposedException>(() =>
            executor.EnqueueBlocking(() => Task.CompletedTask, TestCancellationToken));

        Assert.Equal(nameof(TaskExecutor), exception.ObjectName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-VALIDATION", "null-delegates")]
    public async Task PublicExecutionMethods_RejectNullDelegatesAsync()
    {
        await using var executor = new TaskExecutor();

        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync((Action)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync((Func<Task>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync<int>((Func<int>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.ExecuteAsync<int>((Func<Task<int>>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.ExecuteValueTaskAsync((Func<ValueTask>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.ExecuteValueTaskAsync<int>((Func<ValueTask<int>>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.EnqueueAsync((Action)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.EnqueueAsync((Func<Task>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.EnqueueValueTaskAsync((Func<ValueTask>)null!, TestCancellationToken));
    }

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static TaskCompletionSource NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewCompletionSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        int observed;
        do
        {
            observed = Volatile.Read(ref maximum);
            if (candidate <= observed)
                return;
        }
        while (Interlocked.CompareExchange(ref maximum, candidate, observed) != observed);
    }

    private sealed class ExpectedTaskExecutorException(string message) : Exception(message);

    private sealed class ThrowingLogger : ILogger
    {
        private readonly TaskCompletionSource _called = NewCompletionSource();
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public Task Called => _called.Task;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Interlocked.Increment(ref _callCount);
            _called.TrySetResult();
            throw new InvalidOperationException("Secondary logger fault");
        }
    }
}
