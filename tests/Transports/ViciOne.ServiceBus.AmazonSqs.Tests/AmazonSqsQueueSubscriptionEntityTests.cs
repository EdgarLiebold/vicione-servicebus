using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsQueueSubscriptionEntityTests
{
    [Fact]
    public void Builder_ReusesRepeatedTopicToQueueSubscription()
    {
        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic("orders", true, false);
        QueueHandle queue = builder.CreateQueue("fulfillment", true, false);

        QueueSubscriptionHandle first = builder.CreateQueueSubscription(topic, queue);
        QueueSubscriptionHandle repeated = builder.CreateQueueSubscription(topic, queue);

        Assert.Same(first, repeated);
        QueueSubscription relation = Assert.Single(builder.BuildTopologyLayout().QueueSubscriptions);
        Assert.Equal("orders", relation.Source.EntityName);
        Assert.Equal("fulfillment", relation.Destination.EntityName);
    }

    [Fact]
    public void StructuralComparer_ReusesIndependentEquivalentBrokerDeclarations()
    {
        var first = new QueueSubscriptionEntity(1,
            new TopicEntity(2, "orders", true, false),
            new QueueEntity(3, "fulfillment", true, false));
        var equivalent = new QueueSubscriptionEntity(4,
            new TopicEntity(5, "orders", true, false),
            new QueueEntity(6, "fulfillment", true, false));
        var collection = NewCollection();

        Assert.True(QueueSubscriptionEntity.EntityComparer.Equals(first, equivalent));
        Assert.Equal(QueueSubscriptionEntity.EntityComparer.GetHashCode(first),
            QueueSubscriptionEntity.EntityComparer.GetHashCode(equivalent));
        Assert.Same(first, collection.GetOrAdd(first));
        Assert.Same(first, collection.GetOrAdd(equivalent));
        Assert.Same(first, Assert.Single(collection));
    }

    [Fact]
    public void StructuralComparer_RejectsChangedSourceOrDestinationSettings()
    {
        var first = new QueueSubscriptionEntity(1,
            new TopicEntity(2, "orders", true, false),
            new QueueEntity(3, "fulfillment", true, false));
        var changedSource = new QueueSubscriptionEntity(4,
            new TopicEntity(5, "orders", true, false,
                topicSubscriptionAttributes: new Dictionary<string, object> { ["RawMessageDelivery"] = "false" }),
            new QueueEntity(6, "fulfillment", true, false));
        var changedDestination = new QueueSubscriptionEntity(7,
            new TopicEntity(8, "orders", true, false),
            new QueueEntity(9, "fulfillment", true, false,
                queueAttributes: new Dictionary<string, object> { ["FifoQueue"] = "true" }));

        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(first, changedSource));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(first, changedDestination));
    }

    [Fact]
    public void StructuralComparer_DistinguishesSourceAndDestinationNames()
    {
        var first = new QueueSubscriptionEntity(1,
            new TopicEntity(2, "orders", true, false), new QueueEntity(3, "fulfillment", true, false));
        var changedSource = new QueueSubscriptionEntity(4,
            new TopicEntity(5, "returns", true, false), new QueueEntity(6, "fulfillment", true, false));
        var changedDestination = new QueueSubscriptionEntity(7,
            new TopicEntity(8, "orders", true, false), new QueueEntity(9, "accounting", true, false));

        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(first, changedSource));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(first, changedDestination));
    }

    [Fact]
    public void Collection_RejectsConflictingBrokerRelationAndKeepsFirst()
    {
        var first = new QueueSubscriptionEntity(1,
            new TopicEntity(2, "orders", true, false),
            new QueueEntity(3, "fulfillment", true, false));
        var conflicting = new QueueSubscriptionEntity(4,
            new TopicEntity(5, "orders", true, false,
                topicSubscriptionAttributes: new Dictionary<string, object> { ["RawMessageDelivery"] = "false" }),
            new QueueEntity(6, "fulfillment", true, false));
        var collection = NewCollection();
        collection.GetOrAdd(first);

        Assert.Throws<ArgumentException>(() => collection.GetOrAdd(conflicting));
        Assert.Same(first, Assert.Single(collection));
    }

    [Fact]
    public void NameComparer_UsesBrokerPairAcrossSubscriptionSubtypes()
    {
        var topic = new TopicEntity(1, "orders", true, false);
        var queue = new QueueEntity(2, "fulfillment", true, false);
        var first = new QueueSubscriptionEntity(3, topic, queue);
        var subtype = new DerivedSubscriptionEntity(4, topic, queue);

        Assert.True(QueueSubscriptionEntity.NameComparer.Equals(first, subtype));
        Assert.Equal(QueueSubscriptionEntity.NameComparer.GetHashCode(first),
            QueueSubscriptionEntity.NameComparer.GetHashCode(subtype));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(first, subtype));
    }

    [Fact]
    public void NameComparer_DistinguishesSourceAndDestinationNames()
    {
        var first = new QueueSubscriptionEntity(1,
            new TopicEntity(2, "orders", true, false), new QueueEntity(3, "fulfillment", true, false));
        var changedSource = new QueueSubscriptionEntity(4,
            new TopicEntity(5, "returns", true, false), new QueueEntity(6, "fulfillment", true, false));
        var changedDestination = new QueueSubscriptionEntity(7,
            new TopicEntity(8, "orders", true, false), new QueueEntity(9, "accounting", true, false));

        Assert.False(QueueSubscriptionEntity.NameComparer.Equals(first, changedSource));
        Assert.False(QueueSubscriptionEntity.NameComparer.Equals(first, changedDestination));
    }

    [Fact]
    public void Collection_RejectsDuplicateBrokerPairFromSubscriptionSubtype()
    {
        var topic = new TopicEntity(1, "orders", true, false);
        var queue = new QueueEntity(2, "fulfillment", true, false);
        var first = new QueueSubscriptionEntity(3, topic, queue);
        var collection = NewCollection();
        collection.GetOrAdd(first);

        Assert.Throws<ArgumentException>(() =>
            collection.GetOrAdd(new DerivedSubscriptionEntity(4, topic, queue)));
        Assert.Same(first, Assert.Single(collection));
    }

    static NamedEntityCollection<QueueSubscriptionEntity, QueueSubscriptionHandle> NewCollection() =>
        new(QueueSubscriptionEntity.EntityComparer, QueueSubscriptionEntity.NameComparer);

    sealed class DerivedSubscriptionEntity(long id, TopicEntity topic, QueueEntity queue)
        : QueueSubscriptionEntity(id, topic, queue);
}
