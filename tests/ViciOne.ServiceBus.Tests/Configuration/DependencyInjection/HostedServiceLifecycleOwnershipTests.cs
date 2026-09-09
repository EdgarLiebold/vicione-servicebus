using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.DependencyInjection;

public sealed class HostedServiceLifecycleOwnershipTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "concurrent-start-has-one-depot-owner")]
    public async Task ConcurrentStartCalls_ShareTheDepotStartTaskAsync()
    {
        await using var driver = new HostedServiceLifecycleTestDriver();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Task firstStart = driver.StartAsync(cancellationToken);
        await driver.StartEntered.WaitAsync(cancellationToken);
        Task secondStart = driver.StartAsync(cancellationToken);

        Assert.Equal(1, driver.StartCount);

        driver.CompleteStart();
        await Task.WhenAll(firstStart, secondStart);

        Assert.Equal(1, driver.StartCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "canceled-stop-can-be-retried")]
    public async Task CanceledStop_RemainsRetryableAsync()
    {
        await using var driver = new HostedServiceLifecycleTestDriver(blockFirstStop: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Task start = driver.StartAsync(cancellationToken);
        driver.CompleteStart();
        await start;
        using var stopCancellation = new CancellationTokenSource();

        Task firstStop = driver.StopAsync(stopCancellation.Token);
        await driver.StopEntered.WaitAsync(cancellationToken);
        stopCancellation.Cancel();
        driver.ReleaseStop();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstStop);
        await driver.StopAsync(CancellationToken.None);

        Assert.Equal(2, driver.StopCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "concurrent-stop-has-one-depot-owner")]
    public async Task ConcurrentStopCalls_InvokeTheDepotOnceAsync()
    {
        await using var driver = new HostedServiceLifecycleTestDriver(blockFirstStop: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Task start = driver.StartAsync(cancellationToken);
        driver.CompleteStart();
        await start;

        Task firstStop = driver.StopAsync(CancellationToken.None);
        await driver.StopEntered.WaitAsync(cancellationToken);
        Task secondStop = driver.StopAsync(CancellationToken.None);

        Assert.Equal(1, driver.StopCount);

        driver.ReleaseStop();
        await Task.WhenAll(firstStop, secondStop);

        Assert.Equal(1, driver.StopCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "stop-before-start-is-terminal-without-depot")]
    public async Task StopBeforeStart_IsIdempotentAndRejectsLaterStartupAsync()
    {
        await using var driver = new HostedServiceLifecycleTestDriver();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await driver.StopAsync(cancellationToken);
        await driver.StopAsync(cancellationToken);

        Assert.Equal(0, driver.StartCount);
        Assert.Equal(0, driver.StopCount);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            driver.StartAsync(cancellationToken));
        Assert.Contains("after stopping has begun", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "completed-stop-is-idempotent-with-canceled-caller")]
    public async Task CompletedStop_RemainsIdempotentForAnAlreadyCanceledCallerAsync()
    {
        await using var driver = new HostedServiceLifecycleTestDriver();
        Task start = driver.StartAsync(TestContext.Current.CancellationToken);
        driver.CompleteStart();
        await start;
        await driver.StopAsync(CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await driver.StopAsync(cancellation.Token);

        Assert.Equal(1, driver.StopCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "concurrent-stop-and-dispose-share-owner")]
    public async Task ConcurrentStopAndDispose_InvokeTheDepotOnceAsync()
    {
        await using var driver = new HostedServiceLifecycleTestDriver(blockFirstStop: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Task start = driver.StartAsync(cancellationToken);
        driver.CompleteStart();
        await start;

        Task stop = driver.StopAsync(CancellationToken.None);
        await driver.StopEntered.WaitAsync(cancellationToken);
        Task disposal = driver.DisposeAsync().AsTask();

        Assert.Equal(1, driver.StopCount);
        Assert.False(disposal.IsCompleted);

        driver.ReleaseStop();
        await Task.WhenAll(stop, disposal);
        Assert.Equal(1, driver.StopCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "dispose-timeout-uses-injected-clock")]
    public async Task DisposeTimeout_UsesTheInjectedTimeProviderAsync()
    {
        var timeProvider = new FakeTimeProvider();
        await using var driver = new HostedServiceLifecycleTestDriver(
            blockFirstStop: true,
            stopTimeout: TimeSpan.FromSeconds(5),
            timeProvider: timeProvider);
        Task start = driver.StartAsync(TestContext.Current.CancellationToken);
        driver.CompleteStart();
        await start;

        Task disposal = driver.DisposeAsync().AsTask();
        await driver.StopEntered.WaitAsync(TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => disposal);
        Assert.Equal(1, driver.StopCount);

        await driver.DisposeAsync();
        Assert.Equal(2, driver.StopCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "start-timeout-uses-injected-clock")]
    public async Task StartTimeout_UsesTheInjectedTimeProviderAsync()
    {
        var timeProvider = new FakeTimeProvider();
        var driver = new HostedServiceLifecycleTestDriver(
            startTimeout: TimeSpan.FromHours(1),
            timeProvider: timeProvider);

        try
        {
            Task start = driver.StartAsync(CancellationToken.None);
            await driver.StartEntered.WaitAsync(TestContext.Current.CancellationToken);

            timeProvider.Advance(TimeSpan.FromHours(1));
            await Task.Yield();

            Assert.True(start.IsCompleted);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);
            Assert.Equal(1, driver.StartCount);
        }
        finally
        {
            driver.CompleteStart();
            await driver.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "stop-timeout-uses-injected-clock")]
    public async Task StopTimeout_UsesTheInjectedTimeProviderAsync()
    {
        var timeProvider = new FakeTimeProvider();
        var driver = new HostedServiceLifecycleTestDriver(
            blockFirstStop: true,
            stopTimeout: TimeSpan.FromHours(1),
            timeProvider: timeProvider);

        try
        {
            Task start = driver.StartAsync(CancellationToken.None);
            driver.CompleteStart();
            await start;

            Task stop = driver.StopAsync(CancellationToken.None);
            await driver.StopEntered.WaitAsync(TestContext.Current.CancellationToken);
            timeProvider.Advance(TimeSpan.FromHours(1));
            await Task.Yield();

            Assert.True(stop.IsCompleted);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);
            Assert.Equal(1, driver.StopCount);

            driver.ReleaseStop();
            await driver.StopAsync(CancellationToken.None);
            Assert.Equal(2, driver.StopCount);
        }
        finally
        {
            driver.ReleaseStop();
            await driver.DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "background-start-and-stop-are-serialized")]
    public async Task BackgroundStartAndStop_DoNotOverlapDepotOperationsAsync()
    {
        var driver = new HostedServiceLifecycleTestDriver(
            blockFirstStop: true,
            waitUntilStarted: false);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Task? stop = null;

        try
        {
            Task hostStart = driver.StartAsync(cancellationToken);
            await driver.StartEntered.WaitAsync(cancellationToken);
            stop = driver.StopAsync(CancellationToken.None);

            Assert.True(hostStart.IsCompletedSuccessfully);
            Assert.False(driver.StopEntered.IsCompleted);
            Assert.Equal(0, driver.StopCount);

            driver.CompleteStart();
            await driver.StopEntered.WaitAsync(cancellationToken);
            Assert.Equal(1, driver.StopCount);

            driver.ReleaseStop();
            await stop;
        }
        finally
        {
            driver.CompleteStart();
            driver.ReleaseStop();
            if (stop is not null)
                await stop;

            await driver.DisposeAsync();
        }
    }
}
