using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.DependencyInjection;

public sealed class AsyncBusHandleLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-BUS-LIFECYCLE", "dispose-awaits-start-cancellation")]
    public async Task DisposeDuringStart_WaitsUntilCancellationIsOwnedAsync()
    {
        var driver = new AsyncBusHandleLifecycleTestDriver();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await driver.StartEntered.WaitAsync(cancellationToken);

        Task dispose = driver.DisposeAsync().AsTask();
        await driver.CancellationObserved.WaitAsync(cancellationToken);

        Assert.False(dispose.IsCompleted);

        driver.ReleaseCancellation();
        await dispose;

        Assert.True(driver.StartCompletion.IsCanceled);
        Assert.Equal(0, driver.StopCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASYNC-BUS-LIFECYCLE", "concurrent-dispose-stops-once")]
    public async Task ConcurrentDisposeAfterStart_StopsTheDepotExactlyOnceAsync()
    {
        var driver = new AsyncBusHandleLifecycleTestDriver(blockStop: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await driver.StartEntered.WaitAsync(cancellationToken);
        driver.CompleteStart();
        await driver.StartCompletion.WaitAsync(cancellationToken);

        Task firstDispose = driver.DisposeAsync().AsTask();
        await driver.StopEntered.WaitAsync(cancellationToken);
        Task secondDispose = driver.DisposeAsync().AsTask();

        Assert.Equal(1, driver.StopCount);

        driver.ReleaseStop();
        await Task.WhenAll(firstDispose, secondDispose);

        Assert.Equal(1, driver.StopCount);
    }
}
