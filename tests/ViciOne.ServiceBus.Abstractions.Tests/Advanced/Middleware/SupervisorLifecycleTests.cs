using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Middleware;

public sealed class SupervisorLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "null-agent-is-rejected-without-changing-counts")]
    public void Add_RejectsANullAgentWithoutChangingLifecycleCounts()
    {
        var supervisor = new Supervisor();

        Assert.Equal("agent", Assert.Throws<ArgumentNullException>(() => supervisor.Add(null!)).ParamName);
        Assert.Equal(0, supervisor.TotalCount);
        Assert.Equal(0, supervisor.PeakActiveCount);
        Assert.False(supervisor.Ready.IsCompleted);
        Assert.False(supervisor.Completed.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "invalid-agent-signals-are-rejected-without-changing-counts")]
    public void Add_RejectsMissingAgentSignalsWithoutChangingLifecycleCounts()
    {
        var supervisor = new Supervisor();

        Assert.Equal("agent", Assert.Throws<ArgumentException>(() => supervisor.Add(new MissingReadyAgent())).ParamName);
        Assert.Equal("agent", Assert.Throws<ArgumentException>(() => supervisor.Add(new MissingCompletedAgent())).ParamName);
        Assert.Equal(0, supervisor.TotalCount);
        Assert.Equal(0, supervisor.PeakActiveCount);
        Assert.False(supervisor.Ready.IsCompleted);
        Assert.False(supervisor.Completed.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "stopping-supervisor-rejects-new-agent")]
    public async Task Add_AfterStoppingBeginsRejectsTheAgentWithoutChangingCountsAsync()
    {
        var supervisor = new Supervisor();
        await supervisor.StopAsync(CancellationToken.None);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => supervisor.Add(new Agent()));

        Assert.Equal("The supervisor is stopping and cannot accept another agent.", exception.Message);
        Assert.Equal(0, supervisor.TotalCount);
        Assert.Equal(0, supervisor.PeakActiveCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "ready-fault-propagation")]
    public async Task FaultedChildReadiness_FaultsSupervisorWhileShutdownStillCompletesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        for (var iteration = 0; iteration < 50; iteration++)
        {
            var supervisor = new Supervisor();
            var child = new Agent();
            var expected = new ReadyFailureException(iteration);
            child.SetNotReady(expected);
            supervisor.Add(child);
            supervisor.SetReady();
            ReadyFailureException? exception = null;

            try
            {
                exception = await Assert.ThrowsAsync<ReadyFailureException>(
                    () => supervisor.Ready.WaitAsync(cancellationToken));
            }
            finally
            {
                await supervisor.StopAsync(cancellationToken);
                await supervisor.Completed.WaitAsync(cancellationToken);
            }

            Assert.Same(expected, exception);
            Assert.True(child.Completed.IsCompletedSuccessfully);
            Assert.True(supervisor.Completed.IsCompletedSuccessfully);
            Assert.True(supervisor.Stopping.IsCancellationRequested);
            Assert.True(supervisor.Stopped.IsCancellationRequested);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "empty-stop-completion")]
    public async Task EmptySupervisor_StopsAndCompletesAsync()
    {
        var supervisor = new Supervisor();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        supervisor.SetReady();

        await supervisor.Ready.WaitAsync(cancellationToken);
        await supervisor.StopAsync(cancellationToken);
        await supervisor.Completed.WaitAsync(cancellationToken);

        Assert.True(supervisor.Ready.IsCompletedSuccessfully);
        Assert.True(supervisor.Completed.IsCompletedSuccessfully);
        Assert.True(supervisor.Stopping.IsCancellationRequested);
        Assert.True(supervisor.Stopped.IsCancellationRequested);
        Assert.Equal(0, supervisor.TotalCount);
        Assert.Equal(0, supervisor.PeakActiveCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "nested-shutdown")]
    public async Task NestedSupervisors_StopTheirCompleteAgentChainAsync()
    {
        var outer = new Supervisor();
        var inner = new Supervisor();
        var child = new Agent();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        outer.Add(inner);
        inner.Add(child);
        inner.SetReady();
        outer.SetReady();
        child.SetReady();

        await outer.Ready.WaitAsync(cancellationToken);
        await outer.StopAsync(cancellationToken);
        await outer.Completed.WaitAsync(cancellationToken);

        Assert.True(child.Completed.IsCompletedSuccessfully);
        Assert.True(inner.Completed.IsCompletedSuccessfully);
        Assert.True(outer.Completed.IsCompletedSuccessfully);
        Assert.True(child.Stopped.IsCancellationRequested);
        Assert.True(inner.Stopped.IsCancellationRequested);
        Assert.True(outer.Stopped.IsCancellationRequested);
        Assert.Equal(1, outer.TotalCount);
        Assert.Equal(1, inner.TotalCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "agent-added-after-ready")]
    public async Task AgentAddedAfterReadiness_IsStillStoppedAndCompletedAsync()
    {
        var supervisor = new Supervisor();
        var child = new Agent();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        child.SetReady();
        supervisor.SetReady();
        await supervisor.Ready.WaitAsync(cancellationToken);

        supervisor.Add(child);
        await supervisor.StopAsync(cancellationToken);
        await supervisor.Completed.WaitAsync(cancellationToken);

        Assert.True(child.Ready.IsCompletedSuccessfully);
        Assert.True(child.Completed.IsCompletedSuccessfully);
        Assert.True(child.Stopped.IsCancellationRequested);
        Assert.True(supervisor.Completed.IsCompletedSuccessfully);
        Assert.Equal(1, supervisor.TotalCount);
        Assert.Equal(1, supervisor.PeakActiveCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "faulted-child-completion-remains-observable")]
    public async Task FaultedChildCompletion_RemainsVisibleToEverySupervisorStopAttemptAsync()
    {
        var expected = new ChildCompletionException();
        var child = new FaultedCompletionAgent(expected);
        var supervisor = new Supervisor();
        supervisor.Add(child);

        ChildCompletionException first = await Assert.ThrowsAsync<ChildCompletionException>(
            () => supervisor.StopAsync(CancellationToken.None));
        ChildCompletionException second = await Assert.ThrowsAsync<ChildCompletionException>(
            () => supervisor.StopAsync(CancellationToken.None));
        ChildCompletionException completion = await Assert.ThrowsAsync<ChildCompletionException>(() => supervisor.Completed);

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Same(expected, completion);
        Assert.Equal(2, child.StopCount);
        Assert.True(supervisor.Stopping.IsCancellationRequested);
        Assert.False(supervisor.Stopped.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUPERVISOR-LIFECYCLE", "stop-agent-snapshot-is-read-only")]
    public async Task StopContext_ExposesAReadOnlyChildSnapshotAsync()
    {
        var supervisor = new CapturingSupervisor();
        var child = new Agent();
        child.SetReady();
        supervisor.Add(child);

        await supervisor.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(typeof(IReadOnlyList<IAgent>),
            typeof(StopSupervisorContext).GetProperty(nameof(StopSupervisorContext.Agents))!.PropertyType);
        Assert.NotNull(supervisor.CapturedAgents);
        Assert.False(supervisor.CapturedAgents is IAgent[]);
        Assert.Same(child, Assert.Single(supervisor.CapturedAgents));
    }

    private sealed class ReadyFailureException(int iteration)
        : Exception($"Child readiness failed during iteration {iteration}.")
    {
    }

    private abstract class InvalidAgent : IAgent
    {
        public virtual Task Ready => Task.CompletedTask;

        public virtual Task Completed => Task.CompletedTask;

        public CancellationToken Stopping => CancellationToken.None;

        public CancellationToken Stopped => CancellationToken.None;

        public virtual Task StopAsync(StopContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MissingReadyAgent : InvalidAgent
    {
        public override Task Ready => null!;
    }

    private sealed class MissingCompletedAgent : InvalidAgent
    {
        public override Task Completed => null!;
    }

    private sealed class FaultedCompletionAgent(Exception failure) : InvalidAgent
    {
        private readonly Task _completed = Task.FromException(failure);
        private int _stopCount;

        public override Task Completed => _completed;

        public int StopCount => Volatile.Read(ref _stopCount);

        public override Task StopAsync(StopContext context, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _stopCount);
            return Task.CompletedTask;
        }
    }

    private sealed class ChildCompletionException : Exception;

    private sealed class CapturingSupervisor : Supervisor
    {
        public IReadOnlyList<IAgent>? CapturedAgents { get; private set; }

        protected override Task StopSupervisorAsync(StopSupervisorContext context)
        {
            CapturedAgents = context.Agents;
            return base.StopSupervisorAsync(context);
        }
    }
}
