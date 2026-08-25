using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Internals.Extensions;

public sealed class TaskExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-OR-CANCELED", "completed-and-noncancelable")]
    public async Task OrCanceled_ReturnsTheOriginalTaskWhenCancellationCannotWin()
    {
        Task completed = Task.CompletedTask;
        Task<int> completedValue = Task.FromResult(42);
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Assert.Same(completed, completed.OrCanceled(new CancellationToken(canceled: true)));
        Assert.Same(completedValue, completedValue.OrCanceled(new CancellationToken(canceled: true)));
        Assert.Same(pending.Task, pending.Task.OrCanceled(CancellationToken.None));
        Assert.Equal(42, await completedValue.OrCanceled(CancellationToken.None));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-OR-CANCELED", "cancellation-wins")]
    public async Task OrCanceled_CompletesAsCanceledWithTheExactTokenWhenCancellationWins()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var genericSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();

        Task canceledTask = source.Task.OrCanceled(cancellation.Token);
        Task<int> canceledGenericTask = genericSource.Task.OrCanceled(cancellation.Token);
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
    public async Task OrCanceled_PreservesTheSourceResultAndExactFailureWhenTheSourceWins()
    {
        var expected = new SourceTaskException("source failed");
        using var cancellation = new CancellationTokenSource();

        int result = await Task.FromResult(27).OrCanceled(cancellation.Token);
        SourceTaskException actual = await Assert.ThrowsAsync<SourceTaskException>(() =>
            Task.FromException(expected).OrCanceled(cancellation.Token));

        Assert.Equal(27, result);
        Assert.Same(expected, actual);
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
            _ = task!.OrCanceled(CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = genericTask!.OrCanceled(CancellationToken.None);
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateObservedAndControlFaults(string observedMarker, string controlMarker)
    {
        var source = new TaskCompletionSource();
        using var cancellation = new CancellationTokenSource();
        Task abandoned = source.Task;
        Task canceled = abandoned.OrCanceled(cancellation.Token);
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
