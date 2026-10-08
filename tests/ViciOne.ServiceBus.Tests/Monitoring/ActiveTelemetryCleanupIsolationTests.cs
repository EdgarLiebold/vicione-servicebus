using System.Diagnostics;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class ActiveTelemetryCleanupIsolationTests
{
    public enum Scenario
    {
        Healthy, ActionFailure, ActionTimerFailure, ActionObserverFailure, ActionBothFailure,
        StandaloneTimerFailure, StandaloneObserverFailure, SetupFailure, SetupTimerFailure,
        WaitCancellation, WaitCancellationBothFailure, StandaloneBothFailure, SetupAggregateAndTimerFailure
    }

    [Theory]
    [InlineData(Scenario.Healthy)]
    [InlineData(Scenario.ActionFailure)]
    [InlineData(Scenario.ActionTimerFailure)]
    [InlineData(Scenario.ActionObserverFailure)]
    [InlineData(Scenario.ActionBothFailure)]
    [InlineData(Scenario.StandaloneTimerFailure)]
    [InlineData(Scenario.StandaloneObserverFailure)]
    [InlineData(Scenario.SetupFailure)]
    [InlineData(Scenario.SetupTimerFailure)]
    [InlineData(Scenario.WaitCancellation)]
    [InlineData(Scenario.WaitCancellationBothFailure)]
    [InlineData(Scenario.StandaloneBothFailure)]
    [InlineData(Scenario.SetupAggregateAndTimerFailure)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "public-active-harness-primary-and-all-owned-cleanup")]
    public async Task ActiveScopeReleasesAllOwnersAndPreservesRequiredOutcome(Scenario scenario)
    {
        var actionError = new InvalidOperationException("chosen action failure");
        var setupError = new InvalidOperationException("chosen send connection failure");
        var observationError = new InvalidOperationException("chosen releasing publish connection failure");
        var timerError = new InvalidOperationException("chosen releasing timer failure");
        var consumeError = new InvalidOperationException("chosen releasing consume connection failure");
        bool actionFault = scenario is Scenario.ActionFailure or Scenario.ActionTimerFailure
            or Scenario.ActionObserverFailure or Scenario.ActionBothFailure;
        bool setupFault = scenario is Scenario.SetupFailure or Scenario.SetupTimerFailure or Scenario.SetupAggregateAndTimerFailure;
        bool timerFault = scenario is Scenario.ActionTimerFailure or Scenario.ActionBothFailure or Scenario.StandaloneTimerFailure
            or Scenario.SetupTimerFailure or Scenario.WaitCancellationBothFailure or Scenario.StandaloneBothFailure or Scenario.SetupAggregateAndTimerFailure;
        bool observationFault = scenario is Scenario.ActionObserverFailure or Scenario.ActionBothFailure or Scenario.StandaloneObserverFailure
            or Scenario.WaitCancellationBothFailure or Scenario.StandaloneBothFailure or Scenario.SetupAggregateAndTimerFailure;
        bool waitCancellation = scenario is Scenario.WaitCancellation or Scenario.WaitCancellationBothFailure;
        var clock = new ObservableTimeProvider(new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero), timerFault ? timerError : null);
        using var ownedCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        CancellationToken token = ownedCancellation.Token;
        var state = new HarnessState(clock, token, setupFault ? setupError : null, observationFault ? observationError : null,
            scenario == Scenario.StandaloneBothFailure ? consumeError : null);
        IBaseTestHarness harness = DispatchProxy.Create<IBaseTestHarness, HarnessProxy>();
        ((HarnessProxy)harness).State = state;
        using var caller = new Activity("active-cleanup-caller").Start();
        Assert.NotNull(caller);
        Activity? root = null;
        int starts = 0;
        using var rootListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "ViciOne.ServiceBus.Testing.Monitor",
            Sample = (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = a => { root = a; state.Root = a; starts++; },
        };
        ActivitySource.AddActivityListener(rootListener);
        using var probe = new ActivitySource("active-cleanup-unrelated-probe");
        Assert.Null(probe.StartActivity("before"));
        int actions = 0;
        ActiveTestResult? result = null;
        Task operation = RunAsync();
        try
        {
            Assert.Equal(1, starts);
            Assert.NotNull(root);
            Assert.Equal(caller.Id, root.ParentId);
            Assert.Equal(setupFault ? 0 : 1, actions);
            Assert.Equal(1, clock.TimerCount);
            Assert.Equal(3, state.ConnectionAttempts);
            Assert.Equal(setupFault ? 2 : 3, state.Leases.Count);
            if (actionFault || setupFault)
                Assert.True(operation.IsCompleted);
            else
            {
                Assert.False(operation.IsCompleted);
                Assert.Equal(state.Idle, clock.LastDueTime);
                if (waitCancellation)
                    ownedCancellation.Cancel();
                else
                {
                    clock.Advance(state.Idle - TimeSpan.FromTicks(1));
                    Assert.False(operation.IsCompleted);
                    clock.Advance(TimeSpan.FromTicks(1));
                }
            }
            Exception? escaped = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Empty(state.Registry);
            Assert.All(state.Leases, lease =>
            {
                Assert.Equal(1, lease.ReleaseCount);
                Assert.Same(root, lease.RootAtRelease);
                Assert.False(lease.StoppedAtRelease);
            });
            Assert.Equal(0, clock.ActiveTimerCount);
            Assert.True(root.IsStopped);
            Assert.Null(probe.StartActivity("after"));
            if (actionFault)
                Assert.Same(actionError, escaped);
            else if (scenario == Scenario.SetupAggregateAndTimerFailure)
            {
                AggregateException aggregate = Assert.IsType<AggregateException>(escaped);
                Assert.Collection(aggregate.InnerExceptions, e => Assert.Same(setupError, e), e => Assert.Same(observationError, e));
            }
            else if (setupFault)
                Assert.Same(setupError, escaped);
            else if (waitCancellation)
            {
                OperationCanceledException canceled = Assert.IsAssignableFrom<OperationCanceledException>(escaped);
                Assert.Equal(token, canceled.CancellationToken);
                Assert.False(TestContext.Current.CancellationToken.IsCancellationRequested);
            }
            else if (scenario == Scenario.StandaloneBothFailure)
            {
                AggregateException aggregate = Assert.IsType<AggregateException>(escaped);
                Assert.Collection(aggregate.InnerExceptions, e =>
                {
                    AggregateException ownerFailure = Assert.IsType<AggregateException>(e);
                    Assert.Collection(ownerFailure.InnerExceptions, item => Assert.Same(observationError, item),
                        item => Assert.Same(consumeError, item));
                }, e => Assert.Same(timerError, e));
            }
            else if (observationFault)
                Assert.Same(observationError, escaped);
            else if (timerFault)
                Assert.Same(timerError, escaped);
            else
            {
                Assert.Null(escaped);
                Assert.NotNull(result);
                Assert.Empty(result.Consumed);
                Assert.Empty(result.Published);
                Assert.Empty(result.Sent);
            }
        }
        finally
        {
            if (!operation.IsCompleted)
            {
                ownedCancellation.Cancel();
                clock.Advance(state.Timeout);
                await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            }
        }

        async Task RunAsync()
        {
            result = await harness.ActAsync(() =>
            {
                actions++;
                Assert.Same(root, Activity.Current);
                Assert.Equal(3, state.Registry.Count);
                return actionFault ? Task.FromException(actionError) : Task.CompletedTask;
            }, "actual-active-cleanup", token);
        }
    }

    public sealed class HarnessState(TimeProvider clock, CancellationToken token, Exception? setupFailure, Exception? observerFailure, Exception? consumeFailure)
    {
        public TimeSpan Timeout { get; } = TimeSpan.FromMinutes(10);
        public TimeSpan Idle { get; } = TimeSpan.FromMinutes(1);
        public TimeProvider Clock { get; } = clock;
        public CancellationToken Token { get; } = token;
        public Dictionary<string, object> Registry { get; } = [];
        public List<Lease> Leases { get; } = [];
        public int ConnectionAttempts { get; private set; }
        public Activity? Root { get; set; }
        public ConnectHandle Connect(string name, object observer)
        {
            ConnectionAttempts++;
            if (name == "send" && setupFailure is not null) throw setupFailure;
            Registry.Add(name, observer);
            var lease = new Lease(this, name, observer, name == "publish" ? observerFailure : name == "consume" ? consumeFailure : null);
            Leases.Add(lease);
            return lease;
        }
    }

    public sealed class Lease(HarnessState state, string name, object observer, Exception? failure) : ConnectHandle
    {
        private int _released;
        public int ReleaseCount { get; private set; }
        public Activity? RootAtRelease { get; private set; }
        public bool StoppedAtRelease { get; private set; }
        public void Disconnect() => Dispose();
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0) return;
            RootAtRelease = state.Root;
            StoppedAtRelease = state.Root?.IsStopped ?? throw new InvalidOperationException("Missing actual root at lease release");
            Assert.Same(observer, state.Registry[name]);
            Assert.True(state.Registry.Remove(name));
            ReleaseCount++;
            if (failure is not null) throw failure;
        }
    }

    public class HarnessProxy : DispatchProxy
    {
        public HarnessState State { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (args is { Length: 0 })
                return targetMethod?.Name switch
                {
                    "get_TestTimeout" => State.Timeout,
                    "get_TestInactivityTimeout" => State.Idle,
                    "get_TimeProvider" => State.Clock,
                    _ => throw new InvalidOperationException("Unexpected harness getter: " + targetMethod?.Name),
                };
            if (args is { Length: 1 } && args[0] is { } observer)
                return targetMethod?.Name switch
                {
                    "ConnectConsumeObserver" when observer is IConsumeObserver => State.Connect("consume", observer),
                    "ConnectPublishObserver" when observer is IPublishObserver => State.Connect("publish", observer),
                    "ConnectSendObserver" when observer is ISendObserver => State.Connect("send", observer),
                    _ => throw new InvalidOperationException("Unexpected harness connection: " + targetMethod?.Name),
                };
            throw new InvalidOperationException("Unexpected harness SPI call: " + targetMethod?.Name);
        }
    }
}
