using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Internal;
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
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TIME", "protected-system-time-construction")]
    public void DerivedBusHarness_DefaultConstructionUsesSystemTimeAndOneCancellationScope()
    {
        using var harness = new SystemTimeBusHarness();

        Assert.Same(TimeProvider.System, harness.TimeProvider);
        Assert.Equal(harness.TestCancellationToken, harness.CancellationToken);
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

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "cancel-is-confined-to-current-scope")]
    public async Task Cancel_CancelsOnlyTasksOwnedByTheCurrentScopeAsync()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness(timeProvider)
        {
            TestTimeout = TimeSpan.FromMinutes(1),
        };

        harness.BeginTestScope();
        CancellationToken currentToken = harness.TestCancellationToken;
        TaskCompletionSource<int> currentTask = harness.CreateTaskCompletionSource<int>(TestContext.Current.CancellationToken);

        harness.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await currentTask.Task);
        Assert.True(currentToken.IsCancellationRequested);

        harness.BeginTestScope();
        CancellationToken nextToken = harness.TestCancellationToken;
        TaskCompletionSource<int> nextTask = harness.CreateTaskCompletionSource<int>(TestContext.Current.CancellationToken);

        Assert.False(nextToken.IsCancellationRequested);
        Assert.NotEqual(currentToken, nextToken);
        Assert.True(nextTask.TrySetResult(42));
        Assert.Equal(42, await nextTask.Task);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "cancel-materializes-current-scope")]
    public void Cancel_CancelsTheCurrentScopeBeforeItsTokenIsFirstRequested()
    {
        using var harness = new InMemoryTestHarness
        {
            TestTimeout = TimeSpan.FromMinutes(1),
        };

        harness.BeginTestScope();
        harness.Cancel();

        Assert.True(harness.TestCancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "scope-renews-explicitly-cancelled-budget")]
    public void BeginTestScope_ReplacesAnExplicitlyCancelledBudget()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness(timeProvider)
        {
            TestTimeout = TimeSpan.FromMinutes(1),
        };

        harness.BeginTestScope();
        CancellationToken canceled = harness.TestCancellationToken;
        harness.Cancel();
        Assert.True(canceled.IsCancellationRequested);

        harness.BeginTestScope();
        CancellationToken renewed = harness.TestCancellationToken;

        Assert.False(renewed.IsCancellationRequested);
        Assert.NotEqual(canceled, renewed);
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        Assert.True(renewed.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "expired-budget-does-not-stop-inactivity")]
    public async Task ExpiredTestBudget_DoesNotStopHarnessLifetimeInactivityAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness(timeProvider)
        {
            TestTimeout = TimeSpan.FromMinutes(1),
            TestInactivityTimeout = TimeSpan.FromMinutes(2),
        };

        harness.BeginTestScope();
        CancellationToken firstBudget = harness.TestCancellationToken;
        Task inactivity = harness.InactivityTask;
        await timeProvider.WaitForTimerCountAsync(2);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        Assert.True(firstBudget.IsCancellationRequested);
        Assert.False(inactivity.IsCompleted);

        harness.BeginTestScope();
        CancellationToken secondBudget = harness.TestCancellationToken;
        timeProvider.Advance(TimeSpan.FromSeconds(30));

        Assert.False(secondBudget.IsCancellationRequested);
        Assert.False(inactivity.IsCompleted);

        timeProvider.Advance(TimeSpan.FromSeconds(30));
        await inactivity;

        Assert.True(harness.InactivityToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TIME", "non-positive-timeouts-rejected")]
    public void TimeoutConfiguration_RejectsEveryNonPositiveDuration()
    {
        using var harness = new InMemoryTestHarness();

        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => harness.TestTimeout = TimeSpan.Zero).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => harness.TestTimeout = TimeSpan.FromTicks(-1)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => harness.TestTimeout = Timeout.InfiniteTimeSpan).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => harness.TestInactivityTimeout = TimeSpan.Zero).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => harness.TestInactivityTimeout = TimeSpan.FromTicks(-1)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => harness.TestInactivityTimeout = Timeout.InfiniteTimeSpan).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-RETENTION", "undefined-save-mode-rejected")]
    public void ContextRetention_RejectsUndefinedSaveModes()
    {
        using var harness = new InMemoryTestHarness();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            harness.ContextSaveMode = (TestContextSaveMode)int.MaxValue);

        Assert.Equal("value", exception.ParamName);
        Assert.Equal((TestContextSaveMode)int.MaxValue, exception.ActualValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "disposed-harness-rejects-new-operations")]
    public void DisposedHarness_RejectsEveryOperationThatRequiresOwnedState()
    {
        var harness = new InMemoryTestHarness();
        harness.Dispose();

        Assert.Throws<ObjectDisposedException>(harness.BeginTestScope);
        Assert.Throws<ObjectDisposedException>(() => harness.TestCancellationToken);
        Assert.Throws<ObjectDisposedException>(() =>
        {
            _ = harness.InactivityTask;
        });
        Assert.Throws<ObjectDisposedException>(harness.Cancel);
        Assert.Throws<ObjectDisposedException>(harness.ForceInactive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "completion-observes-scope-and-caller-cancellation")]
    public async Task CompletionSource_ObservesBothHarnessAndCallerCancellationAsync()
    {
        using var callerCancellation = new CancellationTokenSource();
        using var harness = new InMemoryTestHarness();

        Task<int> callerTask = harness.CreateTaskCompletionSource<int>(callerCancellation.Token).Task;
        callerCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => callerTask);

        harness.BeginTestScope();
        Task<int> harnessTask = harness.CreateTaskCompletionSource<int>(TestContext.Current.CancellationToken).Task;
        harness.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harnessTask);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "inactivity-source-disposal-suppresses-callback")]
    public void InactivitySource_DisposalSuppressesTimerCallbacksAndPreventsTimerRecreation()
    {
        var timeProvider = new DisposeCallbackTimeProvider();
        var source = new InactivitySourceProbe(timeProvider);
        var observer = new CountingInactivityObserver();
        using ConnectHandle connection = source.ConnectInactivityObserver(observer);
        source.StartTimerForTest(TimeSpan.FromMinutes(1));

        source.Dispose();
        source.Dispose();

        Assert.Equal(0, observer.EvaluationCount);
        Assert.False(source.IsInactive);
        Assert.True(source.RestartTimerAsync(cancellationToken: TestContext.Current.CancellationToken).IsCompletedSuccessfully);
        Assert.Equal(1, timeProvider.CreatedTimerCount);
        Assert.Throws<ObjectDisposedException>(() => source.ConnectInactivityObserver(observer));
    }

    private sealed class InactivitySourceProbe(TimeProvider timeProvider) : InactivityTestObserver(timeProvider)
    {
        public void StartTimerForTest(TimeSpan timeout) => StartTimer(timeout);
    }

    private sealed class SystemTimeBusHarness : BusTestHarness
    {
        public override string InputQueueName => "system-time";

        public override Uri InputQueueAddress { get; } = new("loopback://localhost/system-time");

        protected override Task<IBusControl> CreateBusAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class CountingInactivityObserver : IInactivityObserver
    {
        private int _evaluationCount;

        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public void RegisterSource(IInactivityObservationSource source)
        {
        }

        public Task EvaluateInactivityAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _evaluationCount);
            return Task.CompletedTask;
        }

        public void ForceInactive()
        {
        }
    }

    private sealed class DisposeCallbackTimeProvider : TimeProvider
    {
        private int _createdTimerCount;

        public int CreatedTimerCount => Volatile.Read(ref _createdTimerCount);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Interlocked.Increment(ref _createdTimerCount);
            return new DisposeCallbackTimer(callback, state);
        }

        private sealed class DisposeCallbackTimer(TimerCallback callback, object? state) : ITimer
        {
            private int _disposed;

            public bool Change(TimeSpan dueTime, TimeSpan period) => Volatile.Read(ref _disposed) == 0;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                    callback(state);
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
