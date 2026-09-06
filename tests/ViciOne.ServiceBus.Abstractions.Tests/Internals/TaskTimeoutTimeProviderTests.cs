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
    public async Task PendingTask_TimesOutOnlyAtTheConfiguredClockBoundaryAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(45);
        var clock = new FakeTimeProvider(StartTime);
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task observed = source.Task.OrTimeoutAsync(timeout, clock, TestContext.Current.CancellationToken);
        Assert.False(observed.IsCompleted);

        clock.Advance(timeout - TimeSpan.FromTicks(1));
        Assert.False(observed.IsCompleted);

        clock.Advance(TimeSpan.FromTicks(1));
        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(() => observed)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        Assert.Contains(nameof(PendingTask_TimesOutOnlyAtTheConfiguredClockBoundaryAsync), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-TIMEOUT-CLOCK", "generic-completion-wins")]
    public async Task GenericTaskCompletion_BeatsTheConfiguredTimeoutAndPreservesTheResultAsync()
    {
        TimeSpan timeout = TimeSpan.FromMinutes(2);
        var clock = new FakeTimeProvider(StartTime);
        var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<int> observed = source.Task.OrTimeoutAsync(timeout, clock, TestContext.Current.CancellationToken);
        source.SetResult(173);

        Assert.Equal(173, await observed);
        clock.Advance(timeout);
        Assert.Equal(173, await observed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-TASK-TIMEOUT-CANCELLATION", "pre-canceled-generic-and-non-generic")]
    public async Task AlreadyCanceledCallerToken_RemainsCancellationWithTheExactTokenAsync(bool generic)
    {
        var clock = new FakeTimeProvider(StartTime);
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            generic
                ? new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously).Task
                    .OrTimeoutAsync(TimeSpan.FromHours(1), clock, source.Token)
                : new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task
                    .OrTimeoutAsync(TimeSpan.FromHours(1), clock, source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-TASK-TIMEOUT-CANCELLATION", "in-flight-generic-and-non-generic")]
    public async Task CallerCancellationWhileWaiting_RemainsCancellationWithTheExactTokenAsync(bool generic)
    {
        var clock = new FakeTimeProvider(StartTime);
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task observed = generic
            ? new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously).Task
                .OrTimeoutAsync(TimeSpan.FromHours(1), clock, source.Token)
            : new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task
                .OrTimeoutAsync(TimeSpan.FromHours(1), clock, source.Token);
        Assert.False(observed.IsCompleted);

        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => observed);
        Assert.Equal(source.Token, exception.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-TIMEOUT-CLOCK", "null-clock-rejected")]
    public void ExplicitNullClock_IsRejectedAtThePublicBoundary()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = source.Task.OrTimeoutAsync(TimeSpan.FromSeconds(1), null!, TestContext.Current.CancellationToken);
        });

        Assert.Equal("timeProvider", exception.ParamName);
    }
}
