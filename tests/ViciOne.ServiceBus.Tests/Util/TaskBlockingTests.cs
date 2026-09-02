using System.Collections.Concurrent;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class TaskBlockingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-BLOCKING", "completed-result")]
    public void Wait_ReturnsTheCompletedResult()
    {
        int result = TaskBlocking.Wait(() => Task.FromResult(42), TestCancellationToken);

        Assert.Equal(42, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-BLOCKING", "later-completion")]
    public async Task Wait_BlocksUntilAnotherThreadCompletesTheTask()
    {
        var pending = NewCompletionSource<int>();
        var callerStarted = NewCompletionSource();
        var waitEntered = NewCompletionSource();
        var returned = NewCompletionSource<int>();
        var callerExited = NewCompletionSource();
        CancellationToken testCancellationToken = TestCancellationToken;

        var caller = new Thread(() =>
        {
            try
            {
                callerStarted.TrySetResult();
                returned.TrySetResult(TaskBlocking.Wait(() =>
                {
                    waitEntered.TrySetResult();
                    return pending.Task;
                }, testCancellationToken));
            }
            catch (Exception exception)
            {
                returned.TrySetException(exception);
            }
            finally
            {
                callerExited.TrySetResult();
            }
        })
        { IsBackground = true };
        caller.Start();

        try
        {
            await callerStarted.Task.WaitAsync(OperationTimeout, testCancellationToken);
            await waitEntered.Task.WaitAsync(OperationTimeout, testCancellationToken);
            Assert.False(returned.Task.IsCompleted);
        }
        finally
        {
            pending.TrySetResult(7);
        }

        Assert.Equal(7, await returned.Task.WaitAsync(OperationTimeout, testCancellationToken));
        await callerExited.Task.WaitAsync(OperationTimeout, testCancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-BLOCKING", "unwrapped-fault")]
    public void Wait_RethrowsTheExactExceptionWithoutAnAggregateWrapper()
    {
        var expectedTask = new ExpectedTaskBlockingException("expected task fault");
        var expectedGenericTask = new ExpectedTaskBlockingException("expected generic task fault");

        ExpectedTaskBlockingException actualTask = Assert.Throws<ExpectedTaskBlockingException>(() =>
            TaskBlocking.Wait(() => Task.FromException(expectedTask), TestCancellationToken));
        ExpectedTaskBlockingException actualGenericTask = Assert.Throws<ExpectedTaskBlockingException>(() =>
            TaskBlocking.Wait(() => Task.FromException<int>(expectedGenericTask), TestCancellationToken));

        Assert.Same(expectedTask, actualTask);
        Assert.Same(expectedGenericTask, actualGenericTask);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-BLOCKING", "external-cancellation")]
    public void Wait_StopsAPendingWaitWhenItsTokenIsAlreadyCanceled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var pending = NewCompletionSource();

        OperationCanceledException exception = Assert.ThrowsAny<OperationCanceledException>(() =>
            TaskBlocking.Wait(pending.Task, cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.False(pending.Task.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-BLOCKING", "cancellation-after-entry")]
    public async Task Wait_StopsAPendingWaitWhenItsTokenIsCanceledAfterEntry()
    {
        var pending = NewCompletionSource();
        var waitEntered = NewCompletionSource();
        var cancellationObserved = NewCompletionSource<CancellationToken>();
        using var cancellation = new CancellationTokenSource();

        var caller = new Thread(() =>
        {
            try
            {
                TaskBlocking.Wait(() =>
                {
                    waitEntered.TrySetResult();
                    return pending.Task;
                }, cancellation.Token);
                cancellationObserved.TrySetException(
                    new InvalidOperationException("The pending wait unexpectedly completed."));
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

        await waitEntered.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.False(cancellationObserved.Task.IsCompleted);
        cancellation.Cancel();

        CancellationToken observed = await cancellationObserved.Task
            .WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.Equal(cancellation.Token, observed);
        Assert.False(pending.Task.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-BLOCKING", "noncapturing-context-and-preservation")]
    public async Task Wait_CompletesWithoutTouchingAnUnneededCallerContext()
    {
        var context = new PumpedSynchronizationContext();
        var release = NewCompletionSource();
        var workStarted = NewCompletionSource();
        var waitEntered = NewCompletionSource();
        var returned = NewCompletionSource<int>();
        var observedContext = NewCompletionSource<SynchronizationContext?>();
        var callerExited = NewCompletionSource();
        CancellationToken testCancellationToken = TestCancellationToken;

        var caller = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                Task<int> work = Task.Run(async () =>
                {
                    workStarted.TrySetResult();
                    await release.Task.ConfigureAwait(false);
                    return 11;
                });

                returned.TrySetResult(TaskBlocking.Wait(() =>
                {
                    waitEntered.TrySetResult();
                    return work;
                }, testCancellationToken));
            }
            catch (Exception exception)
            {
                returned.TrySetException(exception);
            }
            finally
            {
                observedContext.TrySetResult(SynchronizationContext.Current);
                callerExited.TrySetResult();
            }
        })
        { IsBackground = true };
        caller.Start();

        try
        {
            await workStarted.Task.WaitAsync(OperationTimeout, testCancellationToken);
            await waitEntered.Task.WaitAsync(OperationTimeout, testCancellationToken);
            Assert.False(returned.Task.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        Assert.Equal(11, await returned.Task.WaitAsync(OperationTimeout, testCancellationToken));
        Assert.Same(context, await observedContext.Task.WaitAsync(OperationTimeout, testCancellationToken));
        await callerExited.Task.WaitAsync(OperationTimeout, testCancellationToken);
        Assert.Equal(0, context.PostedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-BLOCKING", "captured-context-requires-pump")]
    public async Task Wait_RequiresAnExternalPumpWhenWorkCapturedTheCallerContext()
    {
        var context = new PumpedSynchronizationContext();
        var release = NewCompletionSource<int>();
        var contextCaptured = NewCompletionSource();
        var waitEntered = NewCompletionSource();
        var returned = NewCompletionSource<int>();
        var callerExited = NewCompletionSource();
        CancellationToken testCancellationToken = TestCancellationToken;

        var caller = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                async Task<int> ContinueOnCallerContext()
                {
                    contextCaptured.TrySetResult();
                    return await release.Task + 1;
                }

                Task<int> work = ContinueOnCallerContext();
                returned.TrySetResult(TaskBlocking.Wait(() =>
                {
                    waitEntered.TrySetResult();
                    return work;
                }, testCancellationToken));
            }
            catch (Exception exception)
            {
                returned.TrySetException(exception);
            }
            finally
            {
                callerExited.TrySetResult();
            }
        })
        { IsBackground = true };
        caller.Start();

        var continuationPumped = false;
        try
        {
            await contextCaptured.Task.WaitAsync(OperationTimeout, testCancellationToken);
            await waitEntered.Task.WaitAsync(OperationTimeout, testCancellationToken);
            release.TrySetResult(41);
            await context.Posted.WaitAsync(OperationTimeout, testCancellationToken);

            Assert.False(returned.Task.IsCompleted);
            context.RunOne();
            continuationPumped = true;
        }
        finally
        {
            release.TrySetResult(41);
            if (!continuationPumped)
            {
                await context.Posted.WaitAsync(OperationTimeout, testCancellationToken);
                context.RunOne();
            }
        }

        Assert.Equal(42, await returned.Task.WaitAsync(OperationTimeout, testCancellationToken));
        await callerExited.Task.WaitAsync(OperationTimeout, testCancellationToken);
        Assert.Equal(1, context.PostedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-BLOCKING", "invalid-inputs")]
    public void Wait_RejectsInvalidInputsAtItsPublicBoundary()
    {
        ArgumentNullException task = Assert.Throws<ArgumentNullException>(() =>
            TaskBlocking.Wait((Task)null!, TestCancellationToken));
        ArgumentNullException taskFactory = Assert.Throws<ArgumentNullException>(() =>
            TaskBlocking.Wait((Func<Task>)null!, TestCancellationToken));
        ArgumentNullException genericTaskFactory = Assert.Throws<ArgumentNullException>(() =>
            TaskBlocking.Wait<int>((Func<Task<int>>)null!, TestCancellationToken));
        InvalidOperationException nullTask = Assert.Throws<InvalidOperationException>(() =>
            TaskBlocking.Wait(() => (Task)null!, TestCancellationToken));
        InvalidOperationException nullGenericTask = Assert.Throws<InvalidOperationException>(() =>
            TaskBlocking.Wait(() => (Task<int>)null!, TestCancellationToken));

        Assert.Equal("task", task.ParamName);
        Assert.Equal("taskFactory", taskFactory.ParamName);
        Assert.Equal("taskFactory", genericTaskFactory.ParamName);
        Assert.Equal("The task factory must return a Task.", nullTask.Message);
        Assert.Equal("The task factory must return a Task.", nullGenericTask.Message);
    }

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static TaskCompletionSource NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewCompletionSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ExpectedTaskBlockingException(string message) : Exception(message);

    private sealed class PumpedSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();
        private readonly TaskCompletionSource _posted = NewCompletionSource();
        private int _postedCount;

        public int PostedCount => Volatile.Read(ref _postedCount);

        public Task Posted => _posted.Task;

        public override void Post(SendOrPostCallback callback, object? state)
        {
            ArgumentNullException.ThrowIfNull(callback);

            _queue.Enqueue((callback, state));
            Interlocked.Increment(ref _postedCount);
            _posted.TrySetResult();
        }

        public void RunOne()
        {
            Assert.True(_queue.TryDequeue(out var work), "No posted continuation was available.");
            work.Callback(work.State);
        }
    }
}
