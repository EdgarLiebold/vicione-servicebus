using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Internals.Extensions;

public sealed class TaskExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-OR-CANCELED", "completed-and-noncancelable")]
    public async Task OrCanceled_ReturnsTheOriginalTaskWhenCancellationCannotWinAsync()
    {
        Task completed = Task.CompletedTask;
        Task<int> completedValue = Task.FromResult(42);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Assert.Same(completed, completed.OrCanceledAsync(new CancellationToken(canceled: true)));
        Assert.Same(completedValue, completedValue.OrCanceledAsync(new CancellationToken(canceled: true)));
        Assert.Same(pending.Task, pending.Task.OrCanceledAsync(CancellationToken.None));
        Assert.Equal(42, await completedValue.OrCanceledAsync(CancellationToken.None));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-OR-CANCELED", "cancellation-wins")]
    public async Task OrCanceled_CompletesAsCanceledWithTheExactTokenWhenCancellationWinsAsync()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var genericSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();

        Task canceledTask = source.Task.OrCanceledAsync(cancellation.Token);
        Task<int> canceledGenericTask = genericSource.Task.OrCanceledAsync(cancellation.Token);
        cancellation.Cancel();

        OperationCanceledException first = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledTask);
        OperationCanceledException second = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledGenericTask);

        Assert.True(canceledTask.IsCanceled);
        Assert.True(canceledGenericTask.IsCanceled);
        Assert.Equal(cancellation.Token, first.CancellationToken);
        Assert.Equal(cancellation.Token, second.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-OR-CANCELED", "source-outcome-wins")]
    public async Task OrCanceled_PreservesTheSourceResultAndExactFailureWhenTheSourceWinsAsync()
    {
        var expected = new SourceTaskException("source failed");
        using var cancellation = new CancellationTokenSource();

        int result = await Task.FromResult(27).OrCanceledAsync(cancellation.Token);
        SourceTaskException actual = await Assert.ThrowsAsync<SourceTaskException>(() =>
            Task.FromException(expected).OrCanceledAsync(cancellation.Token));

        Assert.Equal(27, result);
        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-OR-CANCELED", "pending-source-result-and-failure-paths")]
    public async Task OrCanceled_PendingSourcesPreserveExactResultAndFailureAsync()
    {
        var successfulSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failedSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new SourceTaskException("pending source failed");
        using var caller = new CancellationTokenSource();

        Task<int> result = successfulSource.Task.OrCanceledAsync(caller.Token);
        Task fault = failedSource.Task.OrCanceledAsync(caller.Token);
        Assert.False(result.IsCompleted);
        Assert.False(fault.IsCompleted);

        successfulSource.SetResult(73);
        failedSource.SetException(failure);
        Assert.Equal(73, await result);
        Assert.Same(failure, await Assert.ThrowsAsync<SourceTaskException>(() => fault));

    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-OR-CANCELED", "abandoned-fault-is-observed")]
    public void OrCanceled_ObservesAFaultThatArrivesAfterCancellation()
    {
        string observedMarker = $"observed-{Guid.NewGuid():N}";
        string controlMarker = $"unobserved-control-{Guid.NewGuid():N}";
        var publishedMarkers = new ConcurrentQueue<string>();

        void OnUnobserved(object? _, UnobservedTaskExceptionEventArgs args)
        {
            foreach (Exception exception in args.Exception.Flatten().InnerExceptions)
            {
                if (exception.Message == observedMarker || exception.Message == controlMarker)
                    publishedMarkers.Enqueue(exception.Message);
            }

            args.SetObserved();
        }

        TaskScheduler.UnobservedTaskException += OnUnobserved;
        try
        {
            CreateObservedAndControlFaults(observedMarker, controlMarker);

            CollectUntil(() => publishedMarkers.Contains(controlMarker));
            Assert.Contains(controlMarker, publishedMarkers);

            // Give a broken implementation additional complete finalization cycles after the
            // control task proved that this process is publishing unobserved task failures.
            CollectUntil(static () => false, maximumAttempts: 3);

            Assert.DoesNotContain(observedMarker, publishedMarkers);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-OR-CANCELED", "null-task-rejected")]
    public void OrCanceled_RejectsNullTasks()
    {
        Task? task = null;
        Task<int>? genericTask = null;

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = task!.OrCanceledAsync(CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = genericTask!.OrCanceledAsync(CancellationToken.None);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-OUTCOME-TRANSFER", "cancellation-token-identity")]
    public async Task TrySetFromTask_PreservesCancellationTokenIdentityForBothOverloadsAsync()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        CancellationToken expected = cancellationTokenSource.Token;
        var untypedTarget = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var typedTarget = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        untypedTarget.TrySetFromTask(Task.FromCanceled(expected), 27);
        typedTarget.TrySetFromTask(Task.FromCanceled<int>(expected));

        OperationCanceledException untyped = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => untypedTarget.Task);
        OperationCanceledException typed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => typedTarget.Task);

        Assert.Equal(expected, untyped.CancellationToken);
        Assert.Equal(expected, typed.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-OUTCOME-TRANSFER", "exact-success-and-failure-outcomes")]
    public async Task TrySetFromTask_TransfersExactSuccessAndFailureWithoutReplacingTerminalTargetsAsync()
    {
        var failure = new SourceTaskException("transferred source failure");
        var untypedSuccess = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var typedSuccess = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var untypedFailure = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var typedFailure = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        untypedSuccess.TrySetFromTask(Task.CompletedTask, 31);
        typedSuccess.TrySetFromTask(Task.FromResult(32));
        untypedFailure.TrySetFromTask(Task.FromException(failure), 33);
        typedFailure.TrySetFromTask(Task.FromException<int>(failure));

        Assert.Equal(31, await untypedSuccess.Task);
        Assert.Equal(32, await typedSuccess.Task);
        Assert.Same(failure, await Assert.ThrowsAsync<SourceTaskException>(() => untypedFailure.Task));
        Assert.Same(failure, await Assert.ThrowsAsync<SourceTaskException>(() => typedFailure.Task));

        untypedSuccess.TrySetFromTask(Task.FromResult(99), 99);
        typedSuccess.TrySetFromTask(Task.FromResult(99));
        untypedSuccess.TrySetFromTask(Task.FromException(failure), 99);
        typedSuccess.TrySetFromTask(Task.FromException<int>(failure));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        untypedSuccess.TrySetFromTask(Task.FromCanceled(cancellation.Token), 99);
        typedSuccess.TrySetFromTask(Task.FromCanceled<int>(cancellation.Token));
        Assert.Equal(31, await untypedSuccess.Task);
        Assert.Equal(32, await typedSuccess.Task);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateObservedAndControlFaults(string observedMarker, string controlMarker)
    {
        var source = new TaskCompletionSource();
        using var cancellation = new CancellationTokenSource();
        Task abandoned = source.Task;
        Task canceled = abandoned.OrCanceledAsync(cancellation.Token);
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => canceled.GetAwaiter().GetResult());
        source.SetException(new SourceTaskException(observedMarker));

        _ = Task.FromException(new SourceTaskException(controlMarker));
    }

    private static void CollectUntil(Func<bool> condition, int maximumAttempts = 100)
    {
        for (var attempt = 0; attempt < maximumAttempts && !condition(); attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            Thread.Yield();
        }
    }

    private sealed class SourceTaskException(string message) : Exception(message);
}
