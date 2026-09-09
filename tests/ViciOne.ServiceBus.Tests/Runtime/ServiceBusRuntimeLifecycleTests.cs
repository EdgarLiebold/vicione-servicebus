using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Runtime;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Runtime;

public sealed class ServiceBusRuntimeLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-RUNTIME-LIFECYCLE", "cancellation-wins-over-late-readiness")]
    public async Task CanceledStartup_CannotBecomeStartedWhenReadinessCompletesDuringCleanupAsync()
    {
        var driver = new ServiceBusRuntimeLifecycleTestDriver();
        using var source = new CancellationTokenSource();

        Task start = driver.Bus.StartAsync(source.Token);
        await driver.HostStarted.WaitAsync(TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, driver.HostStopCount);
        Assert.Equal(0, driver.PostStartCount);
        Assert.Equal(1, driver.StartFaultedCount);
    }
}
