using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests;

public sealed class BusControlHealthExtensionsTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 25, 1, 2, 3, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "immediate-complete-result")]
    public async Task ExpectedStatusOnFirstObservation_ReturnsTheCompleteExactResultWithoutWaiting()
    {
        var expected = CreateResult(BusHealthStatus.Healthy, "all endpoints ready");
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() => expected);
        var timeProvider = new CountingTimeProvider(StartTime);

        BusHealthResult actual = await bus.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TimeSpan.Zero,
            timeProvider,
            TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.Equal(1, proxy.CheckHealthCount);
        Assert.Equal(0, timeProvider.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "virtual-poll-boundary")]
    public async Task StatusChange_IsObservedOnlyAtTheExactVirtualPollBoundary()
    {
        var expected = CreateResult(BusHealthStatus.Healthy, "ready after poll");
        var initial = CreateResult(BusHealthStatus.Unhealthy, "starting");
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() => initial);

        proxy.HealthResult = () => proxy.CheckHealthCount == 1 ? initial : expected;
        var timeProvider = new CountingTimeProvider(StartTime);

        Task<BusHealthResult> wait = bus.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TimeSpan.FromSeconds(1),
            timeProvider,
            TestContext.Current.CancellationToken);

        Assert.False(wait.IsCompleted);
        Assert.Equal(1, proxy.CheckHealthCount);
        timeProvider.Advance(TimeSpan.FromMilliseconds(99));
        Assert.False(wait.IsCompleted);
        Assert.Equal(1, proxy.CheckHealthCount);

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        BusHealthResult actual = await wait;

        Assert.Same(expected, actual);
        Assert.Equal(2, proxy.CheckHealthCount);
        Assert.Equal(1, timeProvider.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "final-deadline-observation")]
    public async Task ExpectedStatusReachedAtTheExactDeadline_WinsOverTimeout()
    {
        var initial = CreateResult(BusHealthStatus.Degraded, "recovering");
        var expected = CreateResult(BusHealthStatus.Healthy, "recovered");
        var observations = 0;
        (IBusControl bus, _) = CreateBus(() => ++observations == 1 ? initial : expected);
        var timeProvider = new FakeTimeProvider(StartTime);

        Task<BusHealthResult> wait = bus.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TimeSpan.FromMilliseconds(100),
            timeProvider,
            TestContext.Current.CancellationToken);

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Same(expected, await wait);
        Assert.Equal(2, observations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "post-deadline-state-is-too-late")]
    public async Task ExpectedStatusReachedOnlyAfterTheDeadline_DoesNotTurnTheExpiredWaitIntoSuccess()
    {
        var initial = CreateResult(BusHealthStatus.Degraded, "recovering");
        var tooLate = CreateResult(BusHealthStatus.Healthy, "recovered too late");
        var observations = 0;
        (IBusControl bus, _) = CreateBus(() => ++observations == 1 ? initial : tooLate);
        var timeProvider = new FakeTimeProvider(StartTime);

        Task<BusHealthResult> wait = bus.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TimeSpan.FromMilliseconds(100),
            timeProvider,
            TestContext.Current.CancellationToken);

        timeProvider.Advance(TimeSpan.FromMilliseconds(101));

        BusHealthStatusTimeoutException exception =
            await Assert.ThrowsAsync<BusHealthStatusTimeoutException>(() => wait);

        Assert.Same(initial, exception.LastResult);
        Assert.Equal(1, observations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "typed-timeout-diagnostics")]
    public async Task Timeout_ThrowsTypedFailureWithTheCompleteLastObservation()
    {
        var last = CreateResult(BusHealthStatus.Degraded, "endpoint alpha stopped");
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() => last);
        var timeProvider = new FakeTimeProvider(StartTime);
        TimeSpan timeout = TimeSpan.FromMilliseconds(40);

        Task<BusHealthResult> wait = bus.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            timeout,
            timeProvider,
            TestContext.Current.CancellationToken);
        timeProvider.Advance(timeout);

        BusHealthStatusTimeoutException exception =
            await Assert.ThrowsAsync<BusHealthStatusTimeoutException>(() => wait);

        Assert.Equal(BusHealthStatus.Healthy, exception.ExpectedStatus);
        Assert.Equal(BusHealthStatus.Degraded, exception.ActualStatus);
        Assert.Equal(timeout, exception.Timeout);
        Assert.Same(last, exception.LastResult);
        Assert.Contains("endpoint alpha stopped", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2, proxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "zero-timeout-final-result")]
    public async Task ZeroTimeout_ObservesOnceAndThenReportsTheUnexpectedResult()
    {
        var last = CreateResult(BusHealthStatus.Unhealthy, "not started");
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() => last);
        var timeProvider = new CountingTimeProvider(StartTime);

        BusHealthStatusTimeoutException exception = await Assert.ThrowsAsync<BusHealthStatusTimeoutException>(() =>
            bus.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                TimeSpan.Zero,
                timeProvider,
                TestContext.Current.CancellationToken));

        Assert.Same(last, exception.LastResult);
        Assert.Equal(1, proxy.CheckHealthCount);
        Assert.Equal(0, timeProvider.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "exact-cancellation-token")]
    public async Task PendingWait_CancelsWithTheExactCallerToken()
    {
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Unhealthy, "still starting"));
        var timeProvider = new FakeTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();

        Task<BusHealthResult> wait = bus.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            Timeout.InfiniteTimeSpan,
            timeProvider,
            cancellation.Token);
        cancellation.Cancel();

        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);

        Assert.True(wait.IsCanceled);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(1, proxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "pre-cancellation-no-observation")]
    public async Task AlreadyCanceledWait_DoesNotObserveMutableBusState()
    {
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Healthy, "would have succeeded"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            bus.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                TimeSpan.FromSeconds(1),
                new FakeTimeProvider(StartTime),
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, proxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "collection-concurrency-and-order")]
    public async Task CollectionWait_StartsEveryBusAndReturnsCompleteResultsInInputOrder()
    {
        var firstInitial = CreateResult(BusHealthStatus.Unhealthy, "first starting");
        var firstExpected = CreateResult(BusHealthStatus.Healthy, "first ready");
        var secondInitial = CreateResult(BusHealthStatus.Degraded, "second recovering");
        var secondExpected = CreateResult(BusHealthStatus.Healthy, "second ready");
        var firstObservations = 0;
        var secondObservations = 0;
        (IBusControl first, _) = CreateBus(() => ++firstObservations == 1 ? firstInitial : firstExpected);
        (IBusControl second, _) = CreateBus(() => ++secondObservations == 1 ? secondInitial : secondExpected);
        var timeProvider = new FakeTimeProvider(StartTime);

        Task<BusHealthResult[]> wait = new[] { first, second }.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TimeSpan.FromSeconds(1),
            timeProvider,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, firstObservations);
        Assert.Equal(1, secondObservations);
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));

        BusHealthResult[] results = await wait;
        Assert.Equal(new[] { firstExpected, secondExpected }, results);
        Assert.Equal(2, firstObservations);
        Assert.Equal(2, secondObservations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "collection-single-enumeration")]
    public async Task CollectionWait_EnumeratesTheInputExactlyOnce()
    {
        var expected = CreateResult(BusHealthStatus.Healthy, "ready");
        (IBusControl bus, _) = CreateBus(() => expected);
        var enumerationCount = 0;

        IEnumerable<IBusControl> Enumerate()
        {
            enumerationCount++;
            yield return bus;
        }

        BusHealthResult[] results = await Enumerate().WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TimeSpan.Zero,
            new FakeTimeProvider(StartTime),
            TestContext.Current.CancellationToken);

        Assert.Equal(1, enumerationCount);
        Assert.Equal(new[] { expected }, results);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "empty-collection")]
    public async Task EmptyCollection_CompletesWithAnEmptyResult()
    {
        BusHealthResult[] results = await Array.Empty<IBusControl>().WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TimeSpan.FromSeconds(1),
            new FakeTimeProvider(StartTime),
            TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "collection-pre-cancellation-no-observation")]
    public async Task AlreadyCanceledCollectionWait_DoesNotObserveAnyBus()
    {
        (IBusControl first, BusControlProxy firstProxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Healthy, "first would have succeeded"));
        (IBusControl second, BusControlProxy secondProxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Healthy, "second would have succeeded"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new[] { first, second }.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                TimeSpan.FromSeconds(1),
                new FakeTimeProvider(StartTime),
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, firstProxy.CheckHealthCount);
        Assert.Equal(0, secondProxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "single-null-bus")]
    public async Task NullBus_IsRejected()
    {
        IBusControl? missingBus = null;

        Assert.Equal("busControl", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            missingBus!.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                TimeSpan.Zero,
                TimeProvider.System,
                TestContext.Current.CancellationToken))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "single-null-time-provider")]
    public async Task NullTimeProvider_IsRejectedBeforePolling()
    {
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Healthy, "ready"));

        Assert.Equal("timeProvider", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            bus.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                TimeSpan.Zero,
                null!,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(0, proxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "single-invalid-status")]
    public async Task UndefinedExpectedStatus_IsRejectedBeforePolling()
    {
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Healthy, "ready"));

        Assert.Equal("expectedStatus", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            bus.WaitForHealthStatusAsync(
                (BusHealthStatus)123,
                TimeSpan.Zero,
                TimeProvider.System,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(0, proxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "single-invalid-timeout")]
    public async Task InvalidTimeout_IsRejectedBeforePolling()
    {
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Healthy, "ready"));

        Assert.Equal("timeout", (await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            bus.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                TimeSpan.FromTicks(-2),
                TimeProvider.System,
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal(0, proxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "collection-null")]
    public void NullCollection_IsRejected()
    {
        IEnumerable<IBusControl>? missing = null;

        Assert.Equal("busControls", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = missing!.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                TimeSpan.Zero,
                TimeProvider.System,
                TestContext.Current.CancellationToken);
        }).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "collection-null-time-provider")]
    public void NullCollectionTimeProvider_IsRejectedBeforeStartingAnyWait()
    {
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Healthy, "ready"));

        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new[] { bus }.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                TimeSpan.Zero,
                null!,
                TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal(0, proxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "collection-invalid-status")]
    public void UndefinedCollectionExpectedStatus_IsRejectedBeforeStartingAnyWait()
    {
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Healthy, "ready"));

        Assert.Equal("expectedStatus", Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new[] { bus }.WaitForHealthStatusAsync(
                (BusHealthStatus)123,
                TimeSpan.Zero,
                TimeProvider.System,
                TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal(0, proxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "collection-invalid-timeout")]
    public void InvalidCollectionTimeout_IsRejectedBeforeStartingAnyWait()
    {
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Healthy, "ready"));

        Assert.Equal("timeout", Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new[] { bus }.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                TimeSpan.FromTicks(-2),
                TimeProvider.System,
                TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal(0, proxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "collection-null-element")]
    public void NullCollectionElement_IsRejectedBeforeStartingAnyWait()
    {
        (IBusControl bus, BusControlProxy proxy) = CreateBus(() =>
            CreateResult(BusHealthStatus.Healthy, "ready"));
        IBusControl[] withNull = [bus, null!];

        Assert.Equal("busControls", Assert.Throws<ArgumentException>(() =>
        {
            _ = withNull.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                TimeSpan.Zero,
                TimeProvider.System,
                TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal(0, proxy.CheckHealthCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH-WAIT", "system-time-convenience")]
    public async Task SystemTimeConvenienceOverloads_ReturnCompleteImmediateResults()
    {
        var expected = CreateResult(BusHealthStatus.Healthy, "ready");
        (IBusControl first, _) = CreateBus(() => expected);
        (IBusControl second, _) = CreateBus(() => expected);

        BusHealthResult timeoutResult = await first.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TimeSpan.Zero,
            TestContext.Current.CancellationToken);
        BusHealthResult cancellationResult = await first.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TestContext.Current.CancellationToken);
        BusHealthResult[] collectionTimeoutResults = await new[] { first, second }.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TimeSpan.Zero,
            TestContext.Current.CancellationToken);
        BusHealthResult[] collectionCancellationResults = await new[] { first, second }.WaitForHealthStatusAsync(
            BusHealthStatus.Healthy,
            TestContext.Current.CancellationToken);

        Assert.Same(expected, timeoutResult);
        Assert.Same(expected, cancellationResult);
        Assert.Equal(new[] { expected, expected }, collectionTimeoutResults);
        Assert.Equal(new[] { expected, expected }, collectionCancellationResults);
    }

    private static (IBusControl Bus, BusControlProxy Proxy) CreateBus(Func<BusHealthResult> healthResult)
    {
        IBusControl bus = DispatchProxy.Create<IBusControl, BusControlProxy>();
        var proxy = (BusControlProxy)(object)bus;
        proxy.HealthResult = healthResult;
        return (bus, proxy);
    }

    private static BusHealthResult CreateResult(BusHealthStatus status, string description)
    {
        var endpoints = new Dictionary<string, EndpointHealthResult>();

        return status switch
        {
            BusHealthStatus.Healthy => BusHealthResult.Healthy(description, endpoints),
            BusHealthStatus.Degraded => BusHealthResult.Degraded(description, CreateFailure(description), endpoints),
            BusHealthStatus.Unhealthy => BusHealthResult.Unhealthy(description, CreateFailure(description), endpoints),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    private static Exception CreateFailure(string description) =>
        new InvalidOperationException(description);

    private class BusControlProxy : DispatchProxy
    {
        public required Func<BusHealthResult> HealthResult { get; set; }

        public int CheckHealthCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IBusControl.CheckHealth))
            {
                CheckHealthCount++;
                return HealthResult();
            }

            throw new NotSupportedException($"Unexpected bus member: {targetMethod?.Name}");
        }
    }

    private sealed class CountingTimeProvider(DateTimeOffset startTime) : FakeTimeProvider(startTime)
    {
        public int TimerCount { get; private set; }

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            TimerCount++;
            return base.CreateTimer(callback, state, dueTime, period);
        }
    }
}
