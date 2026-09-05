using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology;

public sealed class TopologyEntityNameTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-NAME", "bounded-temporary-name-has-readable-prefix-and-65-bit-suffix")]
    public void TemporaryQueueName_UsesTheCompleteLengthBudgetAndCollisionResistantSuffix()
    {
        const int MaximumLength = 40;
        var topology = new BoundedConsumeTopology(MaximumLength);

        string name = topology.CreateTemporaryQueueName("deliberately-long-topology-name");

        Assert.Equal(MaximumLength, name.Length);
        Assert.Equal('-', name[MaximumLength - 14]);
        Assert.Matches("^[a-zA-Z0-9_.]+-[a-z0-9]{13}$", name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-NAME", "maximum-length-retains-readable-prefix")]
    public void Construction_RejectsABudgetWithoutRoomForPrefixSeparatorAndHash(int maximumLength)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BoundedConsumeTopology(maximumLength));

        Assert.Equal("maxQueueNameLength", exception.ParamName);
    }

    private sealed class BoundedConsumeTopology(int maximumLength) : ConsumeTopology(maximumLength);
}
