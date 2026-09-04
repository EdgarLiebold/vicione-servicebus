using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.Topology;

public sealed class ConsumerEntityTests
{
    [Theory]
    [InlineData("orders-a", false, "orders-a", false, true)]
    [InlineData("orders-a", false, "orders-b", false, false)]
    [InlineData("orders-a", false, "orders-a", true, false)]
    [InlineData(null, false, null, false, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "consumer-contract-identity")]
    public void EntityComparer_IncludesTopicQueueSelectorConsumerAndSharedIdentity(
        string? leftName,
        bool leftShared,
        string? rightName,
        bool rightShared,
        bool expected)
    {
        ConsumerEntity left = CreateTopicConsumer(leftName, leftShared);
        ConsumerEntity right = CreateTopicConsumer(rightName, rightShared);

        Assert.Equal(expected, ConsumerEntity.EntityComparer.Equals(left, right));
        if (expected)
            Assert.Equal(ConsumerEntity.EntityComparer.GetHashCode(left), ConsumerEntity.EntityComparer.GetHashCode(right));
    }

    [Theory]
    [InlineData("orders-a", "orders-a", true)]
    [InlineData("orders-a", "orders-b", false)]
    [InlineData(null, null, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "named-topic-consumers-coexist")]
    public void NameComparer_PreservesDistinctTopicSubscriptionIdentity(
        string? leftName,
        string? rightName,
        bool expected)
    {
        ConsumerEntity left = CreateTopicConsumer(leftName, shared: false);
        ConsumerEntity right = CreateTopicConsumer(rightName, shared: false);

        Assert.Equal(expected, ConsumerEntity.NameComparer.Equals(left, right));
        if (expected)
            Assert.Equal(ConsumerEntity.NameComparer.GetHashCode(left), ConsumerEntity.NameComparer.GetHashCode(right));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "conflicting-shared-contract-rejected")]
    public void BrokerTopology_RejectsAConflictingSharedContractForTheSameNamedConsumer()
    {
        var builder = new BrokerTopologyBuilderProbe();
        TopicHandle topic = builder.Topic("events");
        _ = builder.Consumer(topic, "subscriber", shared: false);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => builder.Consumer(topic, "subscriber", shared: true));

        Assert.Contains("settings did not match", exception.Message, StringComparison.Ordinal);
    }

    private static ConsumerEntity CreateTopicConsumer(string? consumerName, bool shared) =>
        new(
            1,
            new TopicEntity(1, "events", durable: true, autoDelete: false),
            queue: null!,
            selector: "priority = 'high'",
            consumerName!,
            shared);

    private sealed class BrokerTopologyBuilderProbe : BrokerTopologyBuilder
    {
        public TopicHandle Topic(string name) => CreateTopic(name, durable: true, autoDelete: false);

        public ConsumerHandle Consumer(TopicHandle topic, string consumerName, bool shared) =>
            BindConsumer(topic, queue: null!, selector: null!, consumerName, shared);
    }
}
