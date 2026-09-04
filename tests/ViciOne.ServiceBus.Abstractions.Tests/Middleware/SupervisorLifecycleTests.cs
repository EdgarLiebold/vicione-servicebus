using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware;

public sealed class SupervisorLifecycleTests
{
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

    private sealed class ReadyFailureException(int iteration)
        : Exception($"Child readiness failed during iteration {iteration}.")
    {
    }
}
