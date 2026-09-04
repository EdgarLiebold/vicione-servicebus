using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class RetryBusObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-LIFECYCLE", "pre-stop-cancels-in-flight-retry")]
    public async Task PreStop_CancelsTheStableStoppingTokenBeforeResourcesAreReleasedAsync()
    {
        var observer = new RetryBusObserver();
        CancellationToken stoppingToken = observer.Stopping;

        await observer.PreStopAsync(null!);

        Assert.True(stoppingToken.IsCancellationRequested);

        await observer.PostStopAsync(null!);
        observer.Dispose();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-LIFECYCLE", "failed-bus-lifecycle-cancels-retry")]
    public async Task BusCreationStartAndStopFailures_CancelAndReleaseTheirRetryLifetimeAsync()
    {
        var creationObserver = new RetryBusObserver();
        CancellationToken creationToken = creationObserver.Stopping;
        var startObserver = new RetryBusObserver();
        CancellationToken startToken = startObserver.Stopping;
        var stopObserver = new RetryBusObserver();
        CancellationToken stopToken = stopObserver.Stopping;
        var exception = new ExpectedBusFailureException();

        creationObserver.CreateFaulted(exception);
        await startObserver.StartFaultedAsync(null!, exception);
        await stopObserver.StopFaultedAsync(null!, exception);

        Assert.True(creationToken.IsCancellationRequested);
        Assert.True(startToken.IsCancellationRequested);
        Assert.True(stopToken.IsCancellationRequested);

        creationObserver.Dispose();
        startObserver.Dispose();
        stopObserver.Dispose();
    }

    private sealed class ExpectedBusFailureException : Exception
    {
    }
}
