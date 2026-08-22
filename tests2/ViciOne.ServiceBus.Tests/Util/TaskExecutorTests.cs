using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class TaskExecutorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-RUN", "action")]
    public async Task RunAction_CompletesOnlyAfterTheActionExecuted()
    {
        await using var executor = new TaskExecutor();
        var executed = false;

        await executor.Run(() => { executed = true; }, TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.True(executed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-RUN", "task")]
    public async Task RunTask_CompletesOnlyAfterTheAwaitedWorkCompleted()
    {
        await using var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var executed = false;

        Task run = executor.Run(async () =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            executed = true;
        }, TestCancellationToken);

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
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-RUN", "value-task")]
    public async Task RunValueTask_CompletesOnlyAfterTheAwaitedWorkCompleted()
    {
        await using var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var executed = false;

        Task run = executor.RunValueTask(async () =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            executed = true;
        }, TestCancellationToken);

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
    public async Task ResultOverloads_ReturnTheExecutedDelegateResults()
    {
        await using var executor = new TaskExecutor();

        int synchronous = await executor.Run(() => 11, TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);
        int task = await executor.Run(() => Task.FromResult(12), TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);
        int valueTask = await executor.RunValueTask(() => ValueTask.FromResult(13), TestCancellationToken)
            .WaitAsync(OperationTimeout, TestCancellationToken);

        Assert.Equal(11, synchronous);
        Assert.Equal(12, task);
        Assert.Equal(13, valueTask);
    }

    [Theory]
    [InlineData(false, 0, 1, "concurrencyLimit")]
    [InlineData(true, 0, 1, "prefetchCount")]
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
    public async Task ConcurrencyLimit_BoundsSimultaneouslyActiveWork()
    {
        const int concurrencyLimit = 2;
        await using var executor = new TaskExecutor(concurrencyLimit);
        var saturated = NewCompletionSource();
        var release = NewCompletionSource();
        var active = 0;
        var maximum = 0;

        Task[] work = Enumerable.Range(0, 6)
            .Select(_ => executor.Run(async () =>
            {
                int current = Interlocked.Increment(ref active);
                UpdateMaximum(ref maximum, current);
                if (current == concurrencyLimit)
                    saturated.TrySetResult();

                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                Interlocked.Decrement(ref active);
            }, TestCancellationToken))
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
    public async Task BoundedExecutor_BlocksTheNextWriterWhileCapacityIsOccupied()
    {
        await using var executor = new TaskExecutor(prefetchCount: 1, concurrencyLimit: 1);
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var secondExecuted = false;

        Task first = executor.Run(async () =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        }, TestCancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        Task secondPush = executor.Push(() => { secondExecuted = true; }, TestCancellationToken);

        try
        {
            Assert.False(secondPush.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await first.WaitAsync(OperationTimeout, TestCancellationToken);
        await secondPush.WaitAsync(OperationTimeout, TestCancellationToken);

        await executor.DisposeAsync();
        Assert.True(secondExecuted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-CANCELLATION", "queued-work")]
    public async Task CancellationAfterEnqueue_CancelsTheResultWithoutInvokingTheDelegate()
    {
        await using var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var invoked = false;

        Task first = executor.Run(async () =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        }, TestCancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        using var cancellation = new CancellationTokenSource();
        Task canceled = executor.Run(() => invoked = true, cancellation.Token);
        cancellation.Cancel();
        release.TrySetResult();

        await first.WaitAsync(OperationTimeout, TestCancellationToken);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(canceled.IsCanceled);
        Assert.False(invoked);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-CANCELLATION", "bounded-admission")]
    public async Task CancellationWhileWaitingForCapacity_CancelsAdmissionWithoutInvokingTheDelegate()
    {
        await using var executor = new TaskExecutor(prefetchCount: 1, concurrencyLimit: 1);
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var invoked = false;

        Task first = executor.Run(async () =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        }, TestCancellationToken);
        await started.Task.WaitAsync(OperationTimeout, TestCancellationToken);

        using var cancellation = new CancellationTokenSource();
        Task waiting = executor.Run(() => invoked = true, cancellation.Token);

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
    public async Task DelegateFault_IsPropagatedWithoutAnAggregateWrapper()
    {
        await using var executor = new TaskExecutor();

        var exception = await Assert.ThrowsAsync<ExpectedTaskExecutorException>(() =>
            executor.Run(
                () => Task.FromException(new ExpectedTaskExecutorException("expected")),
                TestCancellationToken));

        Assert.Equal("expected", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-CANCELLATION", "delegate-cancellation")]
    public async Task DelegateCancellation_PreservesTheCancellationTokenAndTaskState()
    {
        await using var executor = new TaskExecutor();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task canceled = executor.Run(() => Task.FromCanceled(cancellation.Token), TestCancellationToken);
        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(canceled.IsCanceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-PUSH", "dispose-drains-work")]
    public async Task Push_ReturnsAfterEnqueueAndDisposeWaitsForTheWork()
    {
        var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var executed = false;

        Task disposal;
        try
        {
            await executor.Push(async () =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                executed = true;
            }, TestCancellationToken).WaitAsync(OperationTimeout, TestCancellationToken);
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
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-PUSH", "value-task-dispose-drains-work")]
    public async Task PushValueTask_ReturnsAfterEnqueueAndDisposeWaitsForWork()
    {
        var executor = new TaskExecutor();
        var started = NewCompletionSource();
        var release = NewCompletionSource();
        var executed = false;

        Task disposal;
        try
        {
            await executor.PushValueTask(async () =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                executed = true;
            }, TestCancellationToken).WaitAsync(OperationTimeout, TestCancellationToken);
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
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-DISPOSAL", "idempotent-and-closed")]
    public async Task Disposal_IsIdempotentAndRejectsNewWork()
    {
        var executor = new TaskExecutor();

        await executor.DisposeAsync();
        await executor.DisposeAsync();

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            executor.Run(() => { }, TestCancellationToken));
        Assert.Equal(nameof(TaskExecutor), exception.ObjectName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-EXECUTOR-VALIDATION", "null-delegates")]
    public async Task PublicExecutionMethods_RejectNullDelegates()
    {
        await using var executor = new TaskExecutor();

        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.Run((Action)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.Run((Func<Task>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.Run<int>((Func<int>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.Run<int>((Func<Task<int>>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.RunValueTask((Func<ValueTask>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.RunValueTask<int>((Func<ValueTask<int>>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.Push((Action)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.Push((Func<Task>)null!, TestCancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            executor.PushValueTask((Func<ValueTask>)null!, TestCancellationToken));
    }

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static TaskCompletionSource NewCompletionSource() =>
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
}
