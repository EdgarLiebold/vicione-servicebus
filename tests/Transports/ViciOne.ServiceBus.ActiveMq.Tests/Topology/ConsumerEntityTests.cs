using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.Topology;

public sealed class ConsumerEntityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "queue-contract-identity")]
    public void QueueEntityComparer_UsesExactTypeNameAndLifecycleSettings()
    {
        var baseline = new QueueEntity(1, "orders", durable: true, autoDelete: false);
        var equivalent = new QueueEntity(2, "orders", durable: true, autoDelete: false);

        Assert.True(QueueEntity.QueueComparer.Equals(baseline, baseline));
        Assert.True(QueueEntity.QueueComparer.Equals(baseline, equivalent));
        Assert.Equal(QueueEntity.QueueComparer.GetHashCode(baseline), QueueEntity.QueueComparer.GetHashCode(equivalent));
        Assert.False(QueueEntity.QueueComparer.Equals(baseline, null));
        Assert.False(QueueEntity.QueueComparer.Equals(null, baseline));
        Assert.True(QueueEntity.QueueComparer.Equals(null, null));
        Assert.False(QueueEntity.QueueComparer.Equals(baseline, new DerivedQueueEntity(3, "orders", true, false)));
        Assert.False(QueueEntity.QueueComparer.Equals(baseline, new QueueEntity(3, "priority", true, false)));
        Assert.False(QueueEntity.QueueComparer.Equals(baseline, new QueueEntity(3, "orders", false, false)));
        Assert.False(QueueEntity.QueueComparer.Equals(baseline, new QueueEntity(3, "orders", true, true)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "queue-name-identity")]
    public void QueueNameComparer_UsesExactTypeAndNameOnly()
    {
        var baseline = new QueueEntity(1, "orders", durable: true, autoDelete: false);
        var sameName = new QueueEntity(2, "orders", durable: false, autoDelete: true);

        Assert.True(QueueEntity.NameComparer.Equals(baseline, baseline));
        Assert.True(QueueEntity.NameComparer.Equals(baseline, sameName));
        Assert.Equal(QueueEntity.NameComparer.GetHashCode(baseline), QueueEntity.NameComparer.GetHashCode(sameName));
        Assert.False(QueueEntity.NameComparer.Equals(baseline, null));
        Assert.False(QueueEntity.NameComparer.Equals(null, baseline));
        Assert.True(QueueEntity.NameComparer.Equals(null, null));
        Assert.False(QueueEntity.NameComparer.Equals(baseline, new DerivedQueueEntity(3, "orders", true, false)));
        Assert.False(QueueEntity.NameComparer.Equals(baseline, new QueueEntity(3, "priority", true, false)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "queue-diagnostic-lifecycle")]
    public void QueueDiagnostics_IncludeOnlyEnabledLifecycleFlags()
    {
        var durable = new QueueEntity(1, "orders", durable: true, autoDelete: false);
        var temporary = new QueueEntity(2, "orders", durable: false, autoDelete: true);

        Assert.Equal("name: orders", durable.ToString());
        Assert.Equal("name: orders, auto-delete", temporary.ToString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "topic-contract-identity")]
    public void TopicEntityComparer_UsesExactTypeNameAndLifecycleSettings()
    {
        var baseline = new TopicEntity(1, "events", durable: true, autoDelete: false);
        var equivalent = new TopicEntity(2, "events", durable: true, autoDelete: false);

        Assert.True(TopicEntity.EntityComparer.Equals(baseline, baseline));
        Assert.True(TopicEntity.EntityComparer.Equals(baseline, equivalent));
        Assert.Equal(TopicEntity.EntityComparer.GetHashCode(baseline), TopicEntity.EntityComparer.GetHashCode(equivalent));
        Assert.False(TopicEntity.EntityComparer.Equals(baseline, null));
        Assert.False(TopicEntity.EntityComparer.Equals(null, baseline));
        Assert.True(TopicEntity.EntityComparer.Equals(null, null));
        Assert.False(TopicEntity.EntityComparer.Equals(baseline, new DerivedTopicEntity(3, "events", true, false)));
        Assert.False(TopicEntity.EntityComparer.Equals(baseline, new TopicEntity(3, "audit", true, false)));
        Assert.False(TopicEntity.EntityComparer.Equals(baseline, new TopicEntity(3, "events", false, false)));
        Assert.False(TopicEntity.EntityComparer.Equals(baseline, new TopicEntity(3, "events", true, true)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "topic-name-identity")]
    public void TopicNameComparer_UsesExactTypeAndNameOnly()
    {
        var baseline = new TopicEntity(1, "events", durable: true, autoDelete: false);
        var sameName = new TopicEntity(2, "events", durable: false, autoDelete: true);

        Assert.True(TopicEntity.NameComparer.Equals(baseline, baseline));
        Assert.True(TopicEntity.NameComparer.Equals(baseline, sameName));
        Assert.Equal(TopicEntity.NameComparer.GetHashCode(baseline), TopicEntity.NameComparer.GetHashCode(sameName));
        Assert.False(TopicEntity.NameComparer.Equals(baseline, null));
        Assert.False(TopicEntity.NameComparer.Equals(null, baseline));
        Assert.True(TopicEntity.NameComparer.Equals(null, null));
        Assert.False(TopicEntity.NameComparer.Equals(baseline, new DerivedTopicEntity(3, "events", true, false)));
        Assert.False(TopicEntity.NameComparer.Equals(baseline, new TopicEntity(3, "audit", true, false)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "topic-diagnostic-lifecycle")]
    public void TopicDiagnostics_IncludeOnlyEnabledLifecycleFlags()
    {
        var durable = new TopicEntity(1, "events", durable: true, autoDelete: false);
        var temporary = new TopicEntity(2, "events", durable: false, autoDelete: true);

        Assert.Equal("name: events, durable", durable.ToString());
        Assert.Equal("name: events, auto-delete", temporary.ToString());
    }

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

        Assert.Contains("settings differ from the existing entity with the same name", exception.Message, StringComparison.Ordinal);
        Assert.Equal("entity", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "consumer-identity-includes-every-broker-setting")]
    public void EntityComparer_DetectsEveryBrokerRelevantConsumerDifference()
    {
        ConsumerEntity baseline = CreateQueuedConsumer();
        ConsumerEntity equivalent = CreateQueuedConsumer();

        Assert.True(ConsumerEntity.EntityComparer.Equals(baseline, baseline));
        Assert.True(ConsumerEntity.EntityComparer.Equals(baseline, equivalent));
        Assert.Equal(ConsumerEntity.EntityComparer.GetHashCode(baseline), ConsumerEntity.EntityComparer.GetHashCode(equivalent));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, null));
        Assert.False(ConsumerEntity.EntityComparer.Equals(null, baseline));
        Assert.True(ConsumerEntity.EntityComparer.Equals(null, null));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, new DerivedConsumerEntity()));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, CreateQueuedConsumer(topicName: "audit")));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, CreateQueuedConsumer(topicDurable: false)));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, CreateQueuedConsumer(topicAutoDelete: true)));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, CreateQueuedConsumer(queueName: "priority")));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, CreateQueuedConsumer(queueDurable: false)));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, CreateQueuedConsumer(queueAutoDelete: true)));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, CreateQueuedConsumer(selector: "priority = 'low'")));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, CreateQueuedConsumer(consumerName: "subscriber-b")));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, CreateQueuedConsumer(shared: false)));
        Assert.False(ConsumerEntity.EntityComparer.Equals(baseline, CreateTopicConsumer("subscriber-a", shared: true)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "consumer-name-identity")]
    public void NameComparer_DistinguishesQueueAndDirectTopicConsumers()
    {
        ConsumerEntity queued = CreateQueuedConsumer();
        ConsumerEntity sameQueue = CreateQueuedConsumer(topicName: "audit", consumerName: "other", shared: false);
        ConsumerEntity otherQueue = CreateQueuedConsumer(queueName: "priority");
        ConsumerEntity direct = CreateTopicConsumer(null, shared: false);

        Assert.True(ConsumerEntity.NameComparer.Equals(queued, sameQueue));
        Assert.Equal(ConsumerEntity.NameComparer.GetHashCode(queued), ConsumerEntity.NameComparer.GetHashCode(sameQueue));
        Assert.False(ConsumerEntity.NameComparer.Equals(queued, otherQueue));
        Assert.False(ConsumerEntity.NameComparer.Equals(queued, direct));
        Assert.False(ConsumerEntity.NameComparer.Equals(queued, null));
        Assert.False(ConsumerEntity.NameComparer.Equals(null, queued));
        Assert.True(ConsumerEntity.NameComparer.Equals(null, null));
        Assert.False(ConsumerEntity.NameComparer.Equals(queued, new DerivedConsumerEntity()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "consumer-diagnostics-omit-absent-values")]
    public void ConsumerDiagnostics_OmitAbsentOptionalValues()
    {
        ConsumerEntity queued = CreateQueuedConsumer();
        var direct = new ConsumerEntity(
            1,
            new TopicEntity(1, "events", durable: true, autoDelete: false),
            queue: null,
            selector: null,
            consumerName: null,
            shared: false);

        Assert.Equal(
            "source: events, destination: orders, selector: priority = 'high', consumerName: subscriber-a",
            queued.ToString());
        Assert.Equal("source: events", direct.ToString());
    }

    private static ConsumerEntity CreateTopicConsumer(string? consumerName, bool shared) =>
        new(
            1,
            new TopicEntity(1, "events", durable: true, autoDelete: false),
            queue: null!,
            selector: "priority = 'high'",
            consumerName!,
            shared);

    private static ConsumerEntity CreateQueuedConsumer(
        string topicName = "events",
        bool topicDurable = true,
        bool topicAutoDelete = false,
        string queueName = "orders",
        bool queueDurable = true,
        bool queueAutoDelete = false,
        string selector = "priority = 'high'",
        string consumerName = "subscriber-a",
        bool shared = true) =>
        new(
            1,
            new TopicEntity(1, topicName, topicDurable, topicAutoDelete),
            new QueueEntity(2, queueName, queueDurable, queueAutoDelete),
            selector,
            consumerName,
            shared);

    private sealed class DerivedQueueEntity(long id, string name, bool durable, bool autoDelete)
        : QueueEntity(id, name, durable, autoDelete);

    private sealed class DerivedTopicEntity(long id, string name, bool durable, bool autoDelete)
        : TopicEntity(id, name, durable, autoDelete);

    private sealed class DerivedConsumerEntity()
        : ConsumerEntity(
            1,
            new TopicEntity(1, "events", durable: true, autoDelete: false),
            new QueueEntity(2, "orders", durable: true, autoDelete: false),
            "priority = 'high'",
            "subscriber-a",
            shared: true);

    private sealed class BrokerTopologyBuilderProbe : BrokerTopologyBuilder
    {
        public TopicHandle Topic(string name) => CreateTopic(name, durable: true, autoDelete: false);

        public ConsumerHandle Consumer(TopicHandle topic, string consumerName, bool shared) =>
            BindConsumer(topic, queue: null!, selector: null!, consumerName, shared);
    }
}
