using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryDelayProviderTests
{
    private static readonly DateTimeOffset StartTime =
        new(2032, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "time-provider-boundary")]
    public async Task RelativeDelay_CompletesOnlyWhenItsTimeProviderReachesTheDeadline()
    {
        TimeSpan interval = TimeSpan.FromMinutes(5);
        var timeProvider = new ObservableTimeProvider(StartTime);
        await using var delayProvider = new InMemoryDelayProvider(timeProvider);

        Task delay = delayProvider.Delay(interval, TestContext.Current.CancellationToken);

        timeProvider.Advance(interval - TimeSpan.FromTicks(1));
        Assert.False(delay.IsCompleted);

        timeProvider.Advance(TimeSpan.FromTicks(1));
        await delay;
        Assert.Equal(StartTime + interval, delayProvider.UtcNow);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "manual-advance-boundary")]
    public async Task Advance_ReleasesOnlyDeadlinesAtOrBeforeTheNewLogicalTime()
    {
        TimeSpan firstInterval = TimeSpan.FromMinutes(1);
        TimeSpan secondInterval = TimeSpan.FromMinutes(2);
        var timeProvider = new ObservableTimeProvider(StartTime);
        await using var delayProvider = new InMemoryDelayProvider(timeProvider);
        Task first = delayProvider.Delay(firstInterval, TestContext.Current.CancellationToken);
        Task second = delayProvider.Delay(secondInterval, TestContext.Current.CancellationToken);

        delayProvider.Advance(firstInterval - TimeSpan.FromTicks(1));
        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);

        delayProvider.Advance(TimeSpan.FromTicks(1));
        await first;
        Assert.False(second.IsCompleted);

        delayProvider.Advance(secondInterval - firstInterval);
        await second;
        Assert.Equal(StartTime + secondInterval, delayProvider.UtcNow);
        Assert.Equal(StartTime, timeProvider.GetUtcNow());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "equal-deadlines")]
    public async Task OneAdvance_ReleasesEveryDelayAtTheSameDeadline()
    {
        TimeSpan interval = TimeSpan.FromHours(1);
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var delayProvider = new InMemoryDelayProvider(timeProvider);
        Task[] delays = Enumerable.Range(0, 32)
            .Select(_ => delayProvider.Delay(StartTime + interval, TestContext.Current.CancellationToken))
            .ToArray();

        delayProvider.Advance(interval);

        await Task.WhenAll(delays);
        Assert.All(delays, delay => Assert.True(delay.IsCompletedSuccessfully));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "subsequent-deadline")]
    public async Task DelayScheduledAfterAdvance_UsesTheAdvancedLogicalTime()
    {
        TimeSpan interval = TimeSpan.FromMinutes(10);
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var delayProvider = new InMemoryDelayProvider(timeProvider);
        Task first = delayProvider.Delay(interval, TestContext.Current.CancellationToken);

        delayProvider.Advance(interval);
        await first;
        Task subsequent = delayProvider.Delay(interval, TestContext.Current.CancellationToken);

        Assert.False(subsequent.IsCompleted);
        delayProvider.Advance(interval);
        await subsequent;
        Assert.Equal(StartTime + interval + interval, delayProvider.UtcNow);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "absolute-deadline")]
    public async Task AbsoluteDeadline_CompletesImmediatelyAtOrBeforeLogicalNow()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        await using var delayProvider = new InMemoryDelayProvider(timeProvider);

        Assert.True(delayProvider.Delay(
            StartTime - TimeSpan.FromTicks(1),
            TestContext.Current.CancellationToken).IsCompletedSuccessfully);
        Assert.True(delayProvider.Delay(StartTime, TestContext.Current.CancellationToken).IsCompletedSuccessfully);
        Assert.Equal(0, timeProvider.ChangeCount);

        Task future = delayProvider.Delay(
            StartTime + TimeSpan.FromTicks(1),
            TestContext.Current.CancellationToken);
        Assert.Equal(1, timeProvider.ChangeCount);
        Assert.False(future.IsCompleted);
        delayProvider.Advance(TimeSpan.FromTicks(1));
        await future;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "pending-cancellation")]
    public async Task PendingCancellation_PreservesTheCallerTokenAndDoesNotBlockTheNextDeadline()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        await using var delayProvider = new InMemoryDelayProvider(timeProvider);
        using var cancellation = new CancellationTokenSource();
        Task canceled = delayProvider.Delay(TimeSpan.FromMinutes(1), cancellation.Token);
        Task later = delayProvider.Delay(TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromMinutes(1), timeProvider.LastDueTime);
        cancellation.Cancel();
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(canceled.IsCanceled);
        Assert.Equal(TimeSpan.FromMinutes(2), timeProvider.LastDueTime);
        delayProvider.Advance(TimeSpan.FromMinutes(2));
        await later;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "pre-cancellation")]
    public async Task AlreadyCanceledToken_IsRejectedBeforeAnyDelayIsRegistered()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var delayProvider = new InMemoryDelayProvider(timeProvider);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException relative = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            delayProvider.Delay(TimeSpan.FromMinutes(1), cancellation.Token));
        OperationCanceledException absolute = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            delayProvider.Delay(StartTime + TimeSpan.FromMinutes(1), cancellation.Token));

        Assert.Equal(cancellation.Token, relative.CancellationToken);
        Assert.Equal(cancellation.Token, absolute.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "disposal")]
    public async Task Disposal_CancelsPendingDelaysReleasesTheTimerAndClosesTheProvider()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delayProvider = new InMemoryDelayProvider(timeProvider);
        Task pending = delayProvider.Delay(TimeSpan.FromDays(1), TestContext.Current.CancellationToken);

        Assert.Equal(1, timeProvider.ActiveTimerCount);
        await delayProvider.DisposeAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);

        Assert.True(pending.IsCanceled);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            delayProvider.Delay(TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken));
        Assert.Throws<ObjectDisposedException>(() => delayProvider.Advance(TimeSpan.FromMinutes(1)));
        Assert.Throws<ObjectDisposedException>(() => _ = delayProvider.UtcNow);
        await delayProvider.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "single-timer-lifetime")]
    public async Task EveryPendingDelaySharesOneTimerForTheProviderLifetime()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delayProvider = new InMemoryDelayProvider(timeProvider);

        Task[] delays = Enumerable.Range(1, 64)
            .Select(index => delayProvider.Delay(
                TimeSpan.FromMinutes(index),
                TestContext.Current.CancellationToken))
            .ToArray();

        Assert.Equal(1, timeProvider.TimerCount);
        Assert.Equal(1, timeProvider.ActiveTimerCount);
        delayProvider.Advance(TimeSpan.FromMinutes(64));
        await Task.WhenAll(delays);
        Assert.Equal(1, timeProvider.TimerCount);

        await delayProvider.DisposeAsync();
        Assert.Equal(0, timeProvider.ActiveTimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "negative-delay-validation")]
    public async Task NegativeRelativeDelay_IsRejectedWithoutMovingLogicalTime()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var delayProvider = new InMemoryDelayProvider(timeProvider);

        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            delayProvider.Delay(TimeSpan.FromTicks(-1), TestContext.Current.CancellationToken));

        Assert.Equal("delay", exception.ParamName);
        Assert.Equal(TimeSpan.FromTicks(-1), exception.ActualValue);
        Assert.Equal(StartTime, delayProvider.UtcNow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "advance-validation")]
    public async Task NonpositiveAdvance_IsRejectedWithoutMovingLogicalTime(long ticks)
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        await using var delayProvider = new InMemoryDelayProvider(timeProvider);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            delayProvider.Advance(TimeSpan.FromTicks(ticks)));

        Assert.Equal("duration", exception.ParamName);
        Assert.Equal(TimeSpan.FromTicks(ticks), exception.ActualValue);
        Assert.Equal(StartTime, delayProvider.UtcNow);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "range-validation")]
    public async Task TimeRangeOverflow_IsRejectedWithoutCorruptingLogicalTime()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.MaxValue - TimeSpan.FromMinutes(1));
        await using var delayProvider = new InMemoryDelayProvider(timeProvider);

        ArgumentOutOfRangeException delay = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            delayProvider.Delay(TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken));
        ArgumentOutOfRangeException advance = Assert.Throws<ArgumentOutOfRangeException>(() =>
            delayProvider.Advance(TimeSpan.FromMinutes(2)));

        Assert.Equal("delay", delay.ParamName);
        Assert.Equal("duration", advance.ParamName);
        Assert.Equal(DateTimeOffset.MaxValue - TimeSpan.FromMinutes(1), delayProvider.UtcNow);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "long-deadline-clamping")]
    public async Task LongAbsoluteDelay_IsRearmedInSupportedTimerIntervals()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var delayProvider = new InMemoryDelayProvider(timeProvider);
        Task delay = delayProvider.Delay(DateTimeOffset.MaxValue, TestContext.Current.CancellationToken);

        Assert.False(delay.IsCompleted);
        Assert.Equal(TimeSpan.FromMilliseconds(uint.MaxValue - 1L), timeProvider.LastDueTime);

        await delayProvider.DisposeAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => delay);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "public-surface")]
    public void PublicSurface_UsesStandardTimeShapesAndHidesImplementationDetails()
    {
        Type provider = typeof(InMemoryDelayProvider);
        Type contract = typeof(IInMemoryDelayProvider);
        MethodInfo[] delays = contract.GetMethods()
            .Where(method => method.Name == nameof(IInMemoryDelayProvider.Delay))
            .OrderBy(method => method.GetParameters()[0].ParameterType.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.True(provider.IsSealed);
        Assert.Equal([0, 1], provider.GetConstructors().Select(constructor => constructor.GetParameters().Length).Order());
        Assert.Equal(typeof(DateTimeOffset), contract.GetProperty(nameof(IInMemoryDelayProvider.UtcNow))!.PropertyType);
        Assert.Equal([typeof(DateTimeOffset), typeof(TimeSpan)],
            delays.Select(method => method.GetParameters()[0].ParameterType));
        Assert.All(delays, method => Assert.Equal(typeof(Task), method.ReturnType));
        Assert.Equal(typeof(void), contract.GetMethod(nameof(IInMemoryDelayProvider.Advance))!.ReturnType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-DELAY", "null-time-provider")]
    public void Construction_RejectsANullTimeProvider()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new InMemoryDelayProvider(null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }
}
