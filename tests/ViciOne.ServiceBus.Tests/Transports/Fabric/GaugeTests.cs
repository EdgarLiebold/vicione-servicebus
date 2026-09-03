using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Fabric;

public sealed class GaugeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-GAUGE-ZERO-ACTIVITY", "removal-awaits-observers")]
    public async Task FinalRemoval_CompletesOnlyAfterTheZeroActivityObserverCompletes()
    {
        var gauge = new Gauge();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        gauge.ZeroActive += async () =>
        {
            entered.TrySetResult();
            await release.Task;
        };
        gauge.Add();

        Task removal = gauge.Remove();
        await entered.Task;

        Assert.False(removal.IsCompleted);

        release.TrySetResult();
        await removal;

        Assert.True(removal.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GAUGE-ZERO-ACTIVITY", "ordered-observer-fault-ownership")]
    public async Task FinalRemoval_InvokesObserversInOrderAndPreservesTheFirstFailure()
    {
        var gauge = new Gauge();
        var expected = new ObserverFailureException();
        var events = new List<string>();
        gauge.ZeroActive += () =>
        {
            events.Add("first");
            return Task.CompletedTask;
        };
        gauge.ZeroActive += () =>
        {
            events.Add("failed");
            return Task.FromException(expected);
        };
        gauge.ZeroActive += () =>
        {
            events.Add("unreachable");
            return Task.CompletedTask;
        };
        gauge.Add();

        ObserverFailureException actual = await Assert.ThrowsAsync<ObserverFailureException>(gauge.Remove);

        Assert.Same(expected, actual);
        Assert.Equal(["first", "failed"], events);
    }

    private sealed class ObserverFailureException : Exception;
}
