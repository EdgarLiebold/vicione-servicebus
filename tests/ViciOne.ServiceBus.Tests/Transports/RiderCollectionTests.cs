using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class RiderCollectionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RIDER-LIFECYCLE", "lookup-requires-valid-name-and-active-generation")]
    public void Lookup_ValidatesTheNameAndRequiresAnActiveGeneration()
    {
        var driver = new RiderCollectionTestDriver();

        Assert.Equal("name", Assert.Throws<ArgumentException>(() => driver.Get(" ")).ParamName);
        Assert.Throws<ConfigurationException>(() => driver.Get("missing"));
        Assert.Throws<ConfigurationException>(() => driver.Get("tracked"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RIDER-LIFECYCLE", "lookup-is-case-insensitive-and-generation-scoped")]
    public async Task Lookup_ReturnsTheActiveRiderAndRejectsAStoppedGenerationAsync()
    {
        var driver = new RiderCollectionTestDriver();
        Assert.Equal(1, driver.Start());

        Assert.Same(driver.TrackedRider, driver.Get("TRACKED"));

        await driver.StopAsync(TestContext.Current.CancellationToken);
        Assert.Throws<ConfigurationException>(() => driver.Get("tracked"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RIDER-LIFECYCLE", "each-started-generation-is-stopped-exactly-once-across-recycle")]
    public async Task RepeatedStartStop_StopsEveryStartedRiderGenerationExactlyOnceAsync()
    {
        var driver = new RiderCollectionTestDriver();

        Assert.Equal(1, driver.Start());
        Assert.Equal([1], driver.StartedGenerations);
        await driver.StopAsync(TestContext.Current.CancellationToken);
        Assert.Equal([1], driver.StoppedGenerations);

        Assert.Equal(1, driver.Start());
        Assert.Equal([1, 2], driver.StartedGenerations);
        await driver.StopAsync(TestContext.Current.CancellationToken);
        Assert.Equal([1, 2], driver.StoppedGenerations);

        await driver.StopAsync(TestContext.Current.CancellationToken);
        Assert.Equal([1, 2], driver.StoppedGenerations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RIDER-LIFECYCLE", "concurrent-start-has-single-generation-owner")]
    public async Task ConcurrentStart_StartsOneRiderGenerationAsync()
    {
        var driver = new RiderCollectionTestDriver();

        int handleCount = await driver.StartConcurrentlyAsync(32, TestContext.Current.CancellationToken);

        Assert.Equal(1, handleCount);
        Assert.Equal([1], driver.StartedGenerations);
        await driver.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RIDER-LIFECYCLE", "concurrent-stop-has-single-generation-owner")]
    public async Task ConcurrentHandleStop_StopsTheUnderlyingGenerationOnceAsync()
    {
        var driver = new RiderCollectionTestDriver();

        await driver.StopHandleConcurrentlyAsync(32, TestContext.Current.CancellationToken);

        Assert.Equal(1, driver.StopInvocationCount);
        Assert.Equal([1], driver.StoppedGenerations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RIDER-LIFECYCLE", "synchronous-stop-failure-allows-retry")]
    public async Task SynchronousHandleStopFailure_DoesNotPoisonTheRetryAsync()
    {
        var driver = new RiderCollectionTestDriver();

        Exception failure = await driver.StopHandleWithSynchronousFailureThenRetryAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal("ScriptedStopException", failure.GetType().Name);
        Assert.Equal(2, driver.StopInvocationCount);
        Assert.Equal([1], driver.StoppedGenerations);
    }
}
