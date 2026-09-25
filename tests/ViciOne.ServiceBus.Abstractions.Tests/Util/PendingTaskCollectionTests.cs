using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Util;

public sealed class PendingTaskCollectionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PENDING-TASK-COMPLETION", "canceled-wait-retains-unfinished-delivery-work")]
    public async Task CanceledWait_DoesNotLoseUnfinishedWorkForTheNextWaitAsync()
    {
        var collection = new PendingTaskCollection(1);
        var work = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        collection.Add(work.Task);

        Task firstWait = collection.CompletedAsync(cancellation.Token);
        Assert.False(firstWait.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstWait);

        Task secondWait = collection.CompletedAsync(TestContext.Current.CancellationToken);
        Assert.False(secondWait.IsCompleted);

        work.SetResult();
        await secondWait.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PENDING-TASK-COMPLETION", "wait-drains-work-added-during-completion")]
    public async Task CompletedAsync_DrainsWorkAddedWhileWaitingAsync()
    {
        var collection = new PendingTaskCollection(2);
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        collection.Add(first.Task);

        Task wait = collection.CompletedAsync(TestContext.Current.CancellationToken);
        collection.Add(second.Task);
        first.SetResult();

        await Assert.ThrowsAsync<TimeoutException>(() =>
            wait.WaitAsync(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));
        second.SetResult();
        await wait.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PENDING-TASK-COMPLETION", "fault-is-reported-after-all-snapshot-work-settles")]
    public async Task CompletedAsync_ReportsFaultOnlyAfterEveryCapturedTaskSettlesAsync()
    {
        var collection = new PendingTaskCollection(2);
        var unfinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("delivery failed");
        collection.Add(Task.FromException(failure));
        collection.Add(unfinished.Task);

        Task wait = collection.CompletedAsync(TestContext.Current.CancellationToken);
        Assert.False(wait.IsCompleted);

        unfinished.SetResult();
        InvalidOperationException observed = await Assert.ThrowsAsync<InvalidOperationException>(() => wait);
        Assert.Same(failure, observed);
        await collection.CompletedAsync(TestContext.Current.CancellationToken);
    }
}
