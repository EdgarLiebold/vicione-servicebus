using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers.Concurrency;

public sealed class ConsumerConcurrencyConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-CONCURRENCY-CONFIG", "single-policy-idempotence-and-conflict")]
    public void ConsumerWidePolicy_IsIdempotentOnlyForTheExactPolicy()
    {
        ConsumerSpecification<Consumer> specification = CreateSpecification();
        ConsumerConcurrencyPolicy policy = ConsumerConcurrencyPolicy.Parallel(2);

        specification.ConcurrencyPolicy = policy;
        specification.ConcurrencyPolicy = ConsumerConcurrencyPolicy.Parallel(2);

        ConfigurationException conflict = Assert.Throws<ConfigurationException>(() =>
        {
            specification.ConcurrencyPolicy = ConsumerConcurrencyPolicy.Serial;
        });
        ArgumentException missingSelector = Assert.Throws<ArgumentException>(() =>
        {
            specification.ConcurrencyPolicy = ConsumerConcurrencyPolicy.Partitioned(4);
        });

        Assert.Contains(nameof(Consumer), conflict.Message, StringComparison.Ordinal);
        Assert.Equal("policy", missingSelector.ParamName);
        Assert.Equal(2, specification.ConcurrentMessageLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-CONCURRENCY-CONFIG", "partition-policy-cannot-layer-or-duplicate")]
    public void TypedPartitionPolicy_CannotBeLayeredOrRegisteredTwice()
    {
        ConsumerSpecification<Consumer> partitioned = CreateSpecification();
        partitioned.UsePartitionedConcurrency<Message, int>(4, static message => message.Key);

        ConfigurationException duplicate = Assert.Throws<ConfigurationException>(() =>
            partitioned.UsePartitionedConcurrency<Message, int>(4, static message => message.Key));
        ConfigurationException globalAfterPartition = Assert.Throws<ConfigurationException>(() =>
        {
            partitioned.ConcurrencyPolicy = ConsumerConcurrencyPolicy.Serial;
        });

        ConsumerSpecification<Consumer> global = CreateSpecification();
        global.ConcurrencyPolicy = ConsumerConcurrencyPolicy.Serial;
        ConfigurationException partitionAfterGlobal = Assert.Throws<ConfigurationException>(() =>
            global.UsePartitionedConcurrency<Message, int>(4, static message => message.Key));

        Assert.Contains(nameof(Message), duplicate.Message, StringComparison.Ordinal);
        Assert.Contains("cannot combine", globalAfterPartition.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot combine", partitionAfterGlobal.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ConsumerSpecification<Consumer> CreateSpecification() =>
        new([new ConsumerMessageSpecification<Consumer, Message>()]);

    private sealed class Consumer;
    private sealed record Message(int Key);
}
