using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Internals;

public sealed class TaskTimeoutTimeProviderTests
{
    private static readonly DateTimeOffset StartTime = new(2037, 8, 9, 10, 11, 12, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-TIMEOUT-CLOCK", "non-generic-exact-boundary")]
    public async Task PendingTask_TimesOutOnlyAtTheConfiguredClockBoundary()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(45);
        var clock = new FakeTimeProvider(StartTime);
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task observed = source.Task.OrTimeout(timeout, clock, TestContext.Current.CancellationToken);
        Assert.False(observed.IsCompleted);

        clock.Advance(timeout - TimeSpan.FromTicks(1));
        Assert.False(observed.IsCompleted);

        clock.Advance(TimeSpan.FromTicks(1));
        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(() => observed)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        Assert.Contains(nameof(PendingTask_TimesOutOnlyAtTheConfiguredClockBoundary), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-TIMEOUT-CLOCK", "generic-completion-wins")]
    public async Task GenericTaskCompletion_BeatsTheConfiguredTimeoutAndPreservesTheResult()
    {
        TimeSpan timeout = TimeSpan.FromMinutes(2);
        var clock = new FakeTimeProvider(StartTime);
        var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<int> observed = source.Task.OrTimeout(timeout, clock, TestContext.Current.CancellationToken);
        source.SetResult(173);

        Assert.Equal(173, await observed);
        clock.Advance(timeout);
        Assert.Equal(173, await observed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-TIMEOUT-CLOCK", "null-clock-rejected")]
    public void ExplicitNullClock_IsRejectedAtThePublicBoundary()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = source.Task.OrTimeout(TimeSpan.FromSeconds(1), null!, TestContext.Current.CancellationToken);
        });

        Assert.Equal("timeProvider", exception.ParamName);
    }
}
