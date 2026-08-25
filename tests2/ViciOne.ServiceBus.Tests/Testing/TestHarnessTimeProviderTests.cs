using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class TestHarnessTimeProviderTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TIME", "direct-harness-budget")]
    public void DirectHarness_UsesItsConfiguredTimeProviderForTheTestBudget()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness(timeProvider)
        {
            TestTimeout = TimeSpan.FromMinutes(1),
        };

        CancellationToken cancellationToken = harness.TestCancellationToken;
        Assert.Same(timeProvider, harness.TimeProvider);
        Assert.False(cancellationToken.IsCancellationRequested);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        Assert.True(cancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TIME", "rolling-timer-restart")]
    public void RollingTimer_RestartMovesTheDeadlineOnTheConfiguredClock()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        var callbackCount = 0;
        using var timer = new RollingTimer(
            _ => Interlocked.Increment(ref callbackCount),
            TimeSpan.FromMinutes(1),
            null,
            timeProvider);

        timer.Start();
        timeProvider.Advance(TimeSpan.FromSeconds(30));
        timer.Restart();
        timeProvider.Advance(TimeSpan.FromSeconds(30));

        Assert.False(timer.Triggered);
        Assert.Equal(0, Volatile.Read(ref callbackCount));

        timeProvider.Advance(TimeSpan.FromSeconds(30));

        Assert.True(timer.Triggered);
        Assert.Equal(1, Volatile.Read(ref callbackCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TIME", "null-provider-rejected")]
    public void ProviderAwareHarnessConstruction_RejectsNull()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new InMemoryTestHarness((TimeProvider)null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "dispose-cancels-pending-observations")]
    public void DirectHarness_DisposeCancelsItsPendingTestBudgetAndIsIdempotent()
    {
        var harness = new InMemoryTestHarness
        {
            TestTimeout = TimeSpan.FromMinutes(1),
        };
        CancellationToken cancellationToken = harness.TestCancellationToken;

        harness.Dispose();
        harness.Dispose();

        Assert.True(cancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "inactivity-observer-disposal")]
    public void InactivityObserver_DisposeCancelsItsTokenAndIsIdempotent()
    {
        var observer = new AsyncInactivityObserver(TimeSpan.FromMinutes(1), CancellationToken.None);
        CancellationToken cancellationToken = observer.InactivityToken;

        observer.Dispose();
        observer.Dispose();

        Assert.True(cancellationToken.IsCancellationRequested);
        Assert.True(observer.InactivityTask.IsCanceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "scope-renews-expired-budget")]
    public void BeginTestScope_ReplacesAnExpiredBudgetWithAFreshProviderBackedToken()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness(timeProvider)
        {
            TestTimeout = TimeSpan.FromMinutes(1),
        };
        CancellationToken expired = harness.TestCancellationToken;
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        Assert.True(expired.IsCancellationRequested);

        harness.BeginTestScope();
        CancellationToken renewed = harness.TestCancellationToken;

        Assert.False(renewed.IsCancellationRequested);
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        Assert.True(renewed.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "scope-moves-live-deadline")]
    public void BeginTestScope_MovesTheDeadlineOfALiveBudgetWithoutReplacingItsToken()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness(timeProvider)
        {
            TestTimeout = TimeSpan.FromMinutes(1),
        };
        CancellationToken original = harness.TestCancellationToken;
        timeProvider.Advance(TimeSpan.FromSeconds(30));

        harness.BeginTestScope();
        CancellationToken continued = harness.TestCancellationToken;
        timeProvider.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(original, continued);
        Assert.False(continued.IsCancellationRequested);

        timeProvider.Advance(TimeSpan.FromSeconds(30));
        Assert.True(continued.IsCancellationRequested);
    }
}
