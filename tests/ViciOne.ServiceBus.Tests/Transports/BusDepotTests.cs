using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class BusDepotTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "constructor-rejects-null-dependencies")]
    public void Constructor_RejectsNullDependencies()
    {
        ArgumentNullException instancesException = Assert.Throws<ArgumentNullException>(BusDepotTestDriver.CreateWithNullInstances);
        ArgumentNullException loggerException = Assert.Throws<ArgumentNullException>(BusDepotTestDriver.CreateWithNullLogger);

        Assert.Equal("instances", instancesException.ParamName);
        Assert.Equal("logger", loggerException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "constructor-rejects-null-instance")]
    public void Constructor_RejectsNullInstance()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(BusDepotTestDriver.CreateWithNullInstance);

        Assert.Equal("instances", exception.ParamName);
        Assert.Contains("cannot contain null values", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "constructor-rejects-duplicate-instance-type")]
    public void Constructor_RejectsDuplicateInstanceType()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(BusDepotTestDriver.CreateWithDuplicateInstanceType);

        Assert.Equal("instances", exception.ParamName);
        Assert.Contains(typeof(IBus).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "empty-start-fails-with-actionable-configuration")]
    public async Task EmptyDepot_StartFailsWithActionableConfigurationAsync()
    {
        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            BusDepotTestDriver.StartEmptyAsync(TestContext.Current.CancellationToken));

        Assert.Contains("No bus instances were found", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AddViciOneServiceBus()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-DEPOT", "empty-stop-is-idempotent")]
    public async Task EmptyDepot_StopIsIdempotentAsync()
    {
        await BusDepotTestDriver.StopEmptyAsync(TestContext.Current.CancellationToken);
        await BusDepotTestDriver.StopEmptyAsync(TestContext.Current.CancellationToken);
    }
}
