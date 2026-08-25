using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class TaskUtilTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-CACHED", "completed-values")]
    public async Task CachedTasks_ExposeTheirDeclaredCompletedValues()
    {
        Assert.True(TaskUtil.Completed.IsCompletedSuccessfully);
        Assert.True(await TaskUtil.True);
        Assert.False(await TaskUtil.False);
        Assert.Null(await TaskUtil.Default<string>());
        Assert.Equal(0, await TaskUtil.Default<int>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-FAULT", "exact-exception")]
    public async Task Faulted_PreservesTheExactException()
    {
        var expected = new ExpectedTaskUtilException("expected fault");

        Task<int> task = TaskUtil.Faulted<int>(expected);
        ExpectedTaskUtilException actual =
            await Assert.ThrowsAsync<ExpectedTaskUtilException>(() => task);

        Assert.Same(expected, actual);
        Assert.True(task.IsFaulted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-CANCELLATION", "cached-task")]
    public async Task Canceled_CreatesACanceledTaskWithACanceledToken()
    {
        Task<int> task = TaskUtil.Canceled<int>();

        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        Assert.True(task.IsCanceled);
        Assert.True(exception.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-COMPLETION-SOURCE", "asynchronous-continuations")]
    public void GetTask_CombinesRequestedOptionsWithAsynchronousContinuations()
    {
        const TaskCreationOptions requested = TaskCreationOptions.AttachedToParent;

        TaskCompletionSource<int> generic = TaskUtil.GetTask<int>(requested);
        TaskCompletionSource<bool> nonGeneric = TaskUtil.GetTask(requested);

        TaskCreationOptions expected = requested | TaskCreationOptions.RunContinuationsAsynchronously;
        Assert.Equal(expected, generic.Task.CreationOptions);
        Assert.Equal(expected, nonGeneric.Task.CreationOptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-CANCELLATION-REGISTRATION", "requires-cancelable-token")]
    public void RegisterTask_RejectsANonCancelableToken()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CancellationToken.None.RegisterTask(out _));

        Assert.Equal("cancellationToken", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-CANCELLATION-REGISTRATION", "completion-signal")]
    public async Task RegisterTask_CompletesItsSignalWhenCancellationIsRequested()
    {
        using var cancellation = new CancellationTokenSource();
        using CancellationTokenRegistration registration =
            cancellation.Token.RegisterTask(out Task cancellationSignal);

        Assert.False(cancellationSignal.IsCompleted);
        cancellation.Cancel();

        await cancellationSignal.WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.True(cancellationSignal.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-CANCELLATION-REGISTRATION", "linked-source")]
    public void RegisterIfCanBeCanceled_CancelsTheTargetSource()
    {
        using var trigger = new CancellationTokenSource();
        using var target = new CancellationTokenSource();
        using CancellationTokenRegistration registration =
            trigger.Token.RegisterIfCanBeCanceled(target);

        trigger.Cancel();

        Assert.True(target.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-CANCELLATION-REGISTRATION", "noncancelable-noop")]
    public void RegisterIfCanBeCanceled_ReturnsAnEmptyRegistrationForANonCancelableToken()
    {
        using var target = new CancellationTokenSource();

        CancellationTokenRegistration registration =
            CancellationToken.None.RegisterIfCanBeCanceled(target);

        Assert.Equal(default, registration);
        Assert.False(target.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-CANCELLATION-REGISTRATION", "null-source")]
    public void RegisterIfCanBeCanceled_RejectsANullTargetForEveryTokenShape()
    {
        using var cancellation = new CancellationTokenSource();

        ArgumentNullException cancelable = Assert.Throws<ArgumentNullException>(() =>
            cancellation.Token.RegisterIfCanBeCanceled(null!));
        ArgumentNullException nonCancelable = Assert.Throws<ArgumentNullException>(() =>
            CancellationToken.None.RegisterIfCanBeCanceled(null!));

        Assert.Equal("source", cancelable.ParamName);
        Assert.Equal("source", nonCancelable.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-COMPLETION-SOURCE", "set-completed")]
    public async Task SetCompleted_IsIdempotentAndRejectsANullSource()
    {
        TaskCompletionSource<bool> source = TaskUtil.GetTask();

        source.SetCompleted();
        source.SetCompleted();

        Assert.True(await source.Task);
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            TaskUtil.SetCompleted(null!));
        Assert.Equal("source", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-AWAIT", "completed-result")]
    public void Await_ReturnsTheCompletedResult()
    {
        int result = TaskUtil.Await(() => Task.FromResult(42), TestCancellationToken);

        Assert.Equal(42, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-AWAIT", "later-completion")]
    public async Task Await_BlocksUntilAnotherThreadCompletesTheTask()
    {
        var pending = NewCompletionSource<int>();
        var callerStarted = NewCompletionSource();
        var awaitEntered = NewCompletionSource();
        var returned = NewCompletionSource<int>();
        var callerExited = NewCompletionSource();
        CancellationToken testCancellationToken = TestCancellationToken;

        var caller = new Thread(() =>
        {
            try
            {
                callerStarted.TrySetResult();
                returned.TrySetResult(TaskUtil.Await(() =>
                {
                    awaitEntered.TrySetResult();
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
            await awaitEntered.Task.WaitAsync(OperationTimeout, testCancellationToken);
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
    [RequirementCoverage("REQ-VSB-TASK-UTIL-AWAIT", "unwrapped-fault")]
    public void Await_RethrowsTheExactExceptionWithoutAnAggregateWrapper()
    {
        var expectedTask = new ExpectedTaskUtilException("expected task fault");
        var expectedGenericTask = new ExpectedTaskUtilException("expected generic task fault");

        ExpectedTaskUtilException actualTask = Assert.Throws<ExpectedTaskUtilException>(() =>
            TaskUtil.Await(() => Task.FromException(expectedTask), TestCancellationToken));
        ExpectedTaskUtilException actualGenericTask = Assert.Throws<ExpectedTaskUtilException>(() =>
        {
            _ = TaskUtil.Await(
                () => Task.FromException<int>(expectedGenericTask),
                TestCancellationToken);
        });

        Assert.Same(expectedTask, actualTask);
        Assert.Same(expectedGenericTask, actualGenericTask);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-AWAIT", "external-cancellation")]
    public void Await_StopsAPendingWaitWhenItsTokenIsAlreadyCanceled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var pending = NewCompletionSource();

        OperationCanceledException exception = Assert.ThrowsAny<OperationCanceledException>(() =>
            TaskUtil.Await(pending.Task, cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.False(pending.Task.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-AWAIT", "noncapturing-context-and-preservation")]
    public async Task Await_CompletesWithoutTouchingAnUnneededCallerContext()
    {
        var context = new PumpedSynchronizationContext();
        var release = NewCompletionSource();
        var workStarted = NewCompletionSource();
        var awaitEntered = NewCompletionSource();
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

                returned.TrySetResult(TaskUtil.Await(() =>
                {
                    awaitEntered.TrySetResult();
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
            await awaitEntered.Task.WaitAsync(OperationTimeout, testCancellationToken);
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
    [RequirementCoverage("REQ-VSB-TASK-UTIL-AWAIT", "captured-context-requires-pump")]
    public async Task Await_RequiresAnExternalPumpWhenWorkCapturedTheCallerContext()
    {
        var context = new PumpedSynchronizationContext();
        var release = NewCompletionSource<int>();
        var contextCaptured = NewCompletionSource();
        var awaitEntered = NewCompletionSource();
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
                returned.TrySetResult(TaskUtil.Await(() =>
                {
                    awaitEntered.TrySetResult();
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
            await awaitEntered.Task.WaitAsync(OperationTimeout, testCancellationToken);
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
    [RequirementCoverage("REQ-VSB-TASK-UTIL-PLATFORM", "no-desktop-windows-reference")]
    public void ProductAssembly_DoesNotReferenceDesktopWindowsFrameworks()
    {
        string[] prohibited =
        [
            "PresentationCore",
            "PresentationFramework",
            "System.Windows.Forms",
            "WindowsBase",
        ];
        string[] references = typeof(TaskUtil).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .ToArray();

        Assert.DoesNotContain(references, reference =>
            prohibited.Contains(reference, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-UTIL-VALIDATION", "invalid-inputs")]
    public void AwaitAndFaulted_RejectInvalidInputsAtTheirPublicBoundary()
    {
        ArgumentNullException task = Assert.Throws<ArgumentNullException>(() =>
            TaskUtil.Await((Task)null!, TestCancellationToken));
        ArgumentNullException taskFactory = Assert.Throws<ArgumentNullException>(() =>
            TaskUtil.Await((Func<Task>)null!, TestCancellationToken));
        ArgumentNullException genericTaskFactory = Assert.Throws<ArgumentNullException>(() =>
            TaskUtil.Await<int>((Func<Task<int>>)null!, TestCancellationToken));
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = TaskUtil.Faulted<int>(null!);
        });
        InvalidOperationException nullTask = Assert.Throws<InvalidOperationException>(() =>
            TaskUtil.Await(() => (Task)null!, TestCancellationToken));
        InvalidOperationException nullGenericTask = Assert.Throws<InvalidOperationException>(() =>
            TaskUtil.Await(() => (Task<int>)null!, TestCancellationToken));

        Assert.Equal("task", task.ParamName);
        Assert.Equal("taskFactory", taskFactory.ParamName);
        Assert.Equal("taskFactory", genericTaskFactory.ParamName);
        Assert.Equal("exception", exception.ParamName);
        Assert.Equal("The taskFactory must return a Task", nullTask.Message);
        Assert.Equal("The taskFactory must return a Task", nullGenericTask.Message);
    }

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static TaskCompletionSource NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewCompletionSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class ExpectedTaskUtilException(string message) : Exception(message);

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
