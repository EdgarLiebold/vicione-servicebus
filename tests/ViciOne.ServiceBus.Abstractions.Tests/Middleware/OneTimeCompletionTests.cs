using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware;

public sealed class OneTimeCompletionTests
{
    [Theory]
    [InlineData("success", false)]
    [InlineData("fault", false)]
    [InlineData("cancel", false)]
    [InlineData("success", true)]
    [InlineData("fault", true)]
    [InlineData("cancel", true)]
    [RequirementCoverage("REQ-VSB-ONE-TIME-SETUP", "terminal-continuation-can-evict-or-retry")]
    public async Task TerminalContinuation_CanStartANewAttemptWithoutChangingTheOriginalResultAsync(string outcome, bool synchronous)
    {
        var payload = new OneTimeContextPayload<Marker>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("setup failure");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        int attempts = 0;
        Task original = payload.Value;
        var scheduler = new ReentrantScheduler();
        Task retry = original.ContinueWith(_ =>
        {
            if (outcome == "success")
                payload.Evict();

            return payload.RunOneTimeAsync(() => new OneTimeSetupMethod(() =>
            {
                Interlocked.Increment(ref attempts);
                return Task.CompletedTask;
            }));
        }, TestContext.Current.CancellationToken, TaskContinuationOptions.None, scheduler).Unwrap();

        if (synchronous)
            CompleteSetup();
        Task returned = payload.RunOneTimeAsync(() => new OneTimeSetupMethod(() =>
        {
            Interlocked.Increment(ref attempts);
            return release.Task;
        }));
        if (!synchronous)
            CompleteSetup();

        Assert.Same(original, returned);
        if (outcome == "fault")
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                original.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)));
        else if (outcome == "cancel")
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                original.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.Equal(cancellation.Token, actual.CancellationToken);
        }
        else
            await original.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        await retry.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(2, Volatile.Read(ref attempts));
        Assert.True(payload.HasValue);
        Assert.NotSame(original, payload.Value);
        Assert.True(payload.Value.IsCompletedSuccessfully);
        Assert.Equal(1, scheduler.Executions);

        void CompleteSetup()
        {
            if (outcome == "fault")
                release.SetException(failure);
            else if (outcome == "cancel")
                release.SetCanceled(cancellation.Token);
            else
                release.SetResult();
        }
    }

    private sealed class Marker;

    // A scheduler may execute queued work immediately. This makes completion-time
    // reentrancy deterministic without relying on a ThreadPool scheduling race.
    private sealed class ReentrantScheduler : TaskScheduler
    {
        public int Executions { get; private set; }

        protected override void QueueTask(Task task)
        {
            Executions++;
            if (!TryExecuteTask(task))
                throw new InvalidOperationException("The terminal continuation could not be executed.");
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

        protected override IEnumerable<Task>? GetScheduledTasks() => [];
    }
}
