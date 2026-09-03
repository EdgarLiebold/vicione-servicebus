using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Consumers.Concurrency;

public sealed class ConsumerConcurrencyPolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-CONCURRENCY", "mode-and-limit-factories")]
    public void Factories_PreserveTheExactModeAndBound()
    {
        ConsumerConcurrencyPolicy parallel = ConsumerConcurrencyPolicy.Parallel(17);
        ConsumerConcurrencyPolicy serial = ConsumerConcurrencyPolicy.Serial;
        ConsumerConcurrencyPolicy partitioned = ConsumerConcurrencyPolicy.Partitioned(23);

        Assert.Equal(ConsumerConcurrencyMode.Parallel, parallel.Mode);
        Assert.Equal(17, parallel.Concurrency);
        Assert.Equal(ConsumerConcurrencyMode.Serial, serial.Mode);
        Assert.Equal(1, serial.Concurrency);
        Assert.Same(serial, ConsumerConcurrencyPolicy.Serial);
        Assert.Equal(ConsumerConcurrencyMode.Partitioned, partitioned.Mode);
        Assert.Equal(23, partitioned.Concurrency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1025)]
    [InlineData(int.MaxValue)]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-CONCURRENCY", "hard-allocation-bound")]
    public void Factories_RejectValuesOutsideTheHardBound(int value)
    {
        Assert.Equal("maximumConcurrency", Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConsumerConcurrencyPolicy.Parallel(value)).ParamName);
        Assert.Equal("partitionCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConsumerConcurrencyPolicy.Partitioned(value)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-CONCURRENCY", "maximum-bound-inclusive")]
    public void MaximumConcurrency_IsAnInclusiveBound()
    {
        ConsumerConcurrencyPolicy parallel = ConsumerConcurrencyPolicy.Parallel(ConsumerConcurrencyPolicy.AbsoluteMaximumConcurrency);
        ConsumerConcurrencyPolicy partitioned = ConsumerConcurrencyPolicy.Partitioned(ConsumerConcurrencyPolicy.AbsoluteMaximumConcurrency);

        Assert.Equal(1024, parallel.Concurrency);
        Assert.Equal(1024, partitioned.Concurrency);
    }
}
