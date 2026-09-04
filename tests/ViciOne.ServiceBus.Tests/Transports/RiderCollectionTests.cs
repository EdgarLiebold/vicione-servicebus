using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class RiderCollectionTests
{
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
}
