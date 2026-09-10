using ViciOne.ServiceBus.Testing.Internal;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class AsyncInactivityObserverTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    [Fact]
    [RequirementCoverage("REQ-VSB-INACTIVITY-OBSERVER", "forced-before-task-materialization")]
    public async Task ForcedBeforeTaskMaterialization_CompletesImmediatelyAsync()
    {
        var observer = CreateObserver();

        observer.ForceInactive();

        await observer.InactivityTask;
        Assert.True(observer.InactivityToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INACTIVITY-OBSERVER", "forced-during-first-interval")]
    public async Task ForcedDuringFirstInterval_CompletesWithoutAdvancingTimeAsync()
    {
        var observer = CreateObserver();
        Task inactivity = observer.InactivityTask;

        Assert.False(inactivity.IsCompleted);

        observer.ForceInactive();

        await inactivity;
        Assert.True(observer.InactivityToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INACTIVITY-OBSERVER", "active-to-inactive-transition")]
    public async Task ConnectedSource_KeepsWaitingUntilItReportsInactivityAsync()
    {
        var observer = CreateObserver();
        var source = new ControlledSource { IsInactive = false };
        observer.RegisterSource(source);
        Task inactivity = observer.InactivityTask;

        await observer.EvaluateInactivityAsync(TestContext.Current.CancellationToken);
        Assert.False(inactivity.IsCompleted);

        source.IsInactive = true;
        await observer.EvaluateInactivityAsync(TestContext.Current.CancellationToken);

        await inactivity;
        Assert.True(observer.InactivityToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INACTIVITY-OBSERVER", "one-query-per-virtual-interval")]
    public async Task EachElapsedVirtualInterval_QueriesTheSourceExactlyOnceAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var observer = new AsyncInactivityObserver(Interval, CancellationToken.None, timeProvider);
        var source = new QueryRecordingSource();
        observer.RegisterSource(source);
        Task inactivity = observer.InactivityTask;

        await timeProvider.WaitForTimerCountAsync(1);
        timeProvider.Advance(Interval);
        await source.FirstQuery;

        Assert.False(inactivity.IsCompleted);
        Assert.Equal(1, source.QueryCount);

        await timeProvider.WaitForTimerCountAsync(2);
        timeProvider.Advance(Interval);
        await inactivity;

        Assert.True(observer.InactivityToken.IsCancellationRequested);
        Assert.Equal(2, source.QueryCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INACTIVITY-OBSERVER", "source-failure-is-visible")]
    public async Task SourceFailure_IsExposedByTheObservationTaskAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var observer = new AsyncInactivityObserver(Interval, CancellationToken.None, timeProvider);
        observer.RegisterSource(new FailingSource());
        Task inactivity = observer.InactivityTask;

        await timeProvider.WaitForTimerCountAsync(1);
        timeProvider.Advance(Interval);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await inactivity);
        Assert.Equal("source failure", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INACTIVITY-OBSERVER", "null-time-provider")]
    public void ProviderAwareConstruction_RejectsNullTimeProvider()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new AsyncInactivityObserver(Interval, CancellationToken.None, null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-INACTIVITY-OBSERVER", "positive-interval-validation")]
    public void Construction_RejectsNonPositiveIntervals(int intervalMilliseconds)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AsyncInactivityObserver(TimeSpan.FromMilliseconds(intervalMilliseconds), CancellationToken.None,
                new ObservableTimeProvider(StartTime)));

        Assert.Equal("timeout", exception.ParamName);
    }

    private static AsyncInactivityObserver CreateObserver() =>
        new(Interval, CancellationToken.None, new ObservableTimeProvider(StartTime));

    private sealed class ControlledSource : IInactivityObservationSource
    {
        public bool IsInactive { get; set; }

        public ConnectHandle ConnectInactivityObserver(IInactivityObserver observer) =>
            throw new NotSupportedException("The observer is connected directly by the test.");
    }

    private sealed class QueryRecordingSource : IInactivityObservationSource
    {
        private readonly TaskCompletionSource<bool> _firstQuery =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _queryCount;

        public Task FirstQuery => _firstQuery.Task;

        public int QueryCount => Volatile.Read(ref _queryCount);

        public bool IsInactive
        {
            get
            {
                int queryCount = Interlocked.Increment(ref _queryCount);
                if (queryCount == 1)
                    _firstQuery.TrySetResult(true);

                return queryCount >= 2;
            }
        }

        public ConnectHandle ConnectInactivityObserver(IInactivityObserver observer) =>
            throw new NotSupportedException("The observer is connected directly by the test.");
    }

    private sealed class FailingSource : IInactivityObservationSource
    {
        public bool IsInactive => throw new InvalidOperationException("source failure");

        public ConnectHandle ConnectInactivityObserver(IInactivityObserver observer) =>
            throw new NotSupportedException("The observer is connected directly by the test.");
    }
}
