using ViciOne.ServiceBus.AmazonSqs.Topology;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsTopologyEntityTests
{
    [Fact]
    public void QueueDiagnostics_ShowExactLifecycleTagsAndAttributeGroups()
    {
        var empty = new QueueEntity(1, "orders", false, false);
        var configured = new QueueEntity(2, "orders", true, true,
            queueAttributes: new Dictionary<string, object> { ["FifoQueue"] = "true" },
            queueSubscriptionAttributes: new Dictionary<string, object> { ["RawMessageDelivery"] = "false" },
            queueTags: new Dictionary<string, string> { ["zone"] = "blue" });

        Assert.Equal("name: orders", empty.ToString());
        Assert.Equal("name: orders, durable, auto-delete, tags: zone=blue, attributes: FifoQueue=true, subscription-attributes: RawMessageDelivery=false",
            configured.ToString());
    }

    [Fact]
    public void TopicDiagnostics_ShowRawDeliveryDefaultAndExplicitSettings()
    {
        var empty = new TopicEntity(1, "orders", false, false);
        var configured = new TopicEntity(2, "orders", true, true,
            topicAttributes: new Dictionary<string, object> { ["KmsMasterKeyId"] = "key-1" },
            topicSubscriptionAttributes: new Dictionary<string, object> { ["rawmessagedelivery"] = "false" },
            topicTags: new Dictionary<string, string> { ["zone"] = "blue" });

        Assert.Equal("name: orders, subscription-attributes: RawMessageDelivery=true", empty.ToString());
        Assert.Equal("name: orders, durable, auto-delete, tags: zone=blue, attributes: KmsMasterKeyId=key-1, subscription-attributes: RawMessageDelivery=false",
            configured.ToString());
    }

    [Fact]
    public void QueueComparer_UsesNameAndBothLifecycleFlags()
    {
        var source = new QueueEntity(1, "orders", true, false);
        var same = new QueueEntity(2, "orders", true, false);
        var renamed = new QueueEntity(3, "other", true, false);
        var nonDurable = new QueueEntity(4, "orders", false, false);
        var autoDelete = new QueueEntity(5, "orders", true, true);
        var subtype = new DerivedQueueEntity("orders", true, false);

        Assert.True(QueueEntity.QueueComparer.Equals(source, same));
        Assert.Equal(QueueEntity.QueueComparer.GetHashCode(source), QueueEntity.QueueComparer.GetHashCode(same));
        Assert.False(QueueEntity.QueueComparer.Equals(source, renamed));
        Assert.False(QueueEntity.QueueComparer.Equals(source, nonDurable));
        Assert.False(QueueEntity.QueueComparer.Equals(source, autoDelete));
        Assert.False(QueueEntity.QueueComparer.Equals(source, subtype));
        Assert.True(QueueEntity.QueueComparer.Equals(source, source));
        Assert.True(QueueEntity.QueueComparer.Equals(null, null));
        Assert.False(QueueEntity.QueueComparer.Equals(source, null));
        Assert.False(QueueEntity.QueueComparer.Equals(null, source));
    }

    [Fact]
    public void QueueComparer_DetectsEveryBrokerMetadataDifference()
    {
        var source = new QueueEntity(1, "orders", true, false,
            queueAttributes: new Dictionary<string, object> { ["FifoQueue"] = "true", ["VisibilityTimeout"] = "30" },
            queueSubscriptionAttributes: new Dictionary<string, object> { ["RawMessageDelivery"] = "false", ["FilterPolicyScope"] = "MessageBody" },
            queueTags: new Dictionary<string, string> { ["zone"] = "blue", ["owner"] = "ops" });
        var same = new QueueEntity(2, "orders", true, false,
            queueAttributes: new Dictionary<string, object> { ["VisibilityTimeout"] = "30", ["FifoQueue"] = "true" },
            queueSubscriptionAttributes: new Dictionary<string, object> { ["FilterPolicyScope"] = "MessageBody", ["rawmessagedelivery"] = "false" },
            queueTags: new Dictionary<string, string> { ["owner"] = "ops", ["zone"] = "blue" });

        Assert.True(QueueEntity.QueueComparer.Equals(source, same));
        Assert.Equal(QueueEntity.QueueComparer.GetHashCode(source), QueueEntity.QueueComparer.GetHashCode(same));
        Assert.False(QueueEntity.QueueComparer.Equals(source, new QueueEntity(3, "orders", true, false,
            queueAttributes: new Dictionary<string, object> { ["FifoQueue"] = "false", ["VisibilityTimeout"] = "30" },
            queueSubscriptionAttributes: source.QueueSubscriptionAttributes, queueTags: source.QueueTags)));
        Assert.False(QueueEntity.QueueComparer.Equals(source, new QueueEntity(4, "orders", true, false,
            queueAttributes: source.QueueAttributes,
            queueSubscriptionAttributes: new Dictionary<string, object> { ["RawMessageDelivery"] = "true", ["FilterPolicyScope"] = "MessageBody" },
            queueTags: source.QueueTags)));
        Assert.False(QueueEntity.QueueComparer.Equals(source, new QueueEntity(5, "orders", true, false,
            queueAttributes: source.QueueAttributes, queueSubscriptionAttributes: source.QueueSubscriptionAttributes,
            queueTags: new Dictionary<string, string> { ["zone"] = "red", ["owner"] = "ops" })));
    }

    [Fact]
    public void QueueComparer_DetectsChangedMetadataKeysWithIdenticalValues()
    {
        var source = new QueueEntity(1, "orders", true, false,
            queueAttributes: new Dictionary<string, object> { ["FifoQueue"] = "true" },
            queueSubscriptionAttributes: new Dictionary<string, object> { ["RawMessageDelivery"] = "false" },
            queueTags: new Dictionary<string, string> { ["zone"] = "blue" });

        Assert.False(QueueEntity.QueueComparer.Equals(source, new QueueEntity(2, "orders", true, false,
            queueAttributes: new Dictionary<string, object> { ["ContentBasedDeduplication"] = "true" },
            queueSubscriptionAttributes: source.QueueSubscriptionAttributes, queueTags: source.QueueTags)));
        Assert.False(QueueEntity.QueueComparer.Equals(source, new QueueEntity(3, "orders", true, false,
            queueAttributes: source.QueueAttributes,
            queueSubscriptionAttributes: new Dictionary<string, object> { ["FilterPolicy"] = "false" },
            queueTags: source.QueueTags)));
        Assert.False(QueueEntity.QueueComparer.Equals(source, new QueueEntity(4, "orders", true, false,
            queueAttributes: source.QueueAttributes, queueSubscriptionAttributes: source.QueueSubscriptionAttributes,
            queueTags: new Dictionary<string, string> { ["owner"] = "blue" })));
    }

    [Fact]
    public void QueueBuilder_RejectsConflictingAttributesWithoutLosingTheFirstDeclaration()
    {
        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        QueueHandle first = builder.CreateQueue("orders", true, false,
            queueAttributes: new Dictionary<string, object> { ["FifoQueue"] = "true" });
        QueueHandle reused = builder.CreateQueue("orders", true, false,
            queueAttributes: new Dictionary<string, object> { ["FifoQueue"] = "true" });

        Assert.Same(first, reused);
        Assert.Throws<ArgumentException>(() => builder.CreateQueue("orders", true, false,
            queueAttributes: new Dictionary<string, object> { ["FifoQueue"] = "false" }));
        Assert.Throws<ArgumentException>(() => builder.CreateQueue("orders", true, false));

        QueueEntity queue = Assert.IsType<QueueEntity>(Assert.Single(builder.BuildTopologyLayout().Queues));
        Assert.Equal(first.Id, queue.Id);
        Assert.Equal("true", queue.QueueAttributes["FifoQueue"]);
    }

    [Fact]
    public void QueueNameComparer_UsesAwsNameAcrossSubtypesWithoutLifecycleFlags()
    {
        var source = new QueueEntity(1, "orders", true, false);
        var changedLifetime = new QueueEntity(2, "orders", false, true);
        var renamed = new QueueEntity(3, "other", true, false);
        var subtype = new DerivedQueueEntity("orders", true, false);

        Assert.True(QueueEntity.NameComparer.Equals(source, changedLifetime));
        Assert.Equal(QueueEntity.NameComparer.GetHashCode(source), QueueEntity.NameComparer.GetHashCode(changedLifetime));
        Assert.False(QueueEntity.NameComparer.Equals(source, renamed));
        Assert.True(QueueEntity.NameComparer.Equals(source, subtype));
        Assert.Equal(QueueEntity.NameComparer.GetHashCode(source), QueueEntity.NameComparer.GetHashCode(subtype));
        Assert.True(QueueEntity.NameComparer.Equals(source, source));
        Assert.True(QueueEntity.NameComparer.Equals(null, null));
        Assert.False(QueueEntity.NameComparer.Equals(source, null));
        Assert.False(QueueEntity.NameComparer.Equals(null, source));
    }

    [Fact]
    public void TopicComparer_UsesNameAndBothLifecycleFlags()
    {
        var source = new TopicEntity(1, "orders", true, false);
        var same = new TopicEntity(2, "orders", true, false);
        var renamed = new TopicEntity(3, "other", true, false);
        var nonDurable = new TopicEntity(4, "orders", false, false);
        var autoDelete = new TopicEntity(5, "orders", true, true);
        var subtype = new DerivedTopicEntity("orders", true, false);

        Assert.True(TopicEntity.EntityComparer.Equals(source, same));
        Assert.Equal(TopicEntity.EntityComparer.GetHashCode(source), TopicEntity.EntityComparer.GetHashCode(same));
        Assert.False(TopicEntity.EntityComparer.Equals(source, renamed));
        Assert.False(TopicEntity.EntityComparer.Equals(source, nonDurable));
        Assert.False(TopicEntity.EntityComparer.Equals(source, autoDelete));
        Assert.False(TopicEntity.EntityComparer.Equals(source, subtype));
        Assert.True(TopicEntity.EntityComparer.Equals(source, source));
        Assert.True(TopicEntity.EntityComparer.Equals(null, null));
        Assert.False(TopicEntity.EntityComparer.Equals(source, null));
        Assert.False(TopicEntity.EntityComparer.Equals(null, source));
    }

    [Fact]
    public void TopicComparer_DetectsEveryBrokerMetadataDifference()
    {
        var source = new TopicEntity(1, "orders", true, false,
            topicAttributes: new Dictionary<string, object> { ["KmsMasterKeyId"] = "key-a", ["DisplayName"] = "Orders" },
            topicSubscriptionAttributes: new Dictionary<string, object> { ["RawMessageDelivery"] = "false", ["FilterPolicyScope"] = "MessageBody" },
            topicTags: new Dictionary<string, string> { ["zone"] = "blue", ["owner"] = "ops" });
        var same = new TopicEntity(2, "orders", true, false,
            topicAttributes: new Dictionary<string, object> { ["DisplayName"] = "Orders", ["KmsMasterKeyId"] = "key-a" },
            topicSubscriptionAttributes: new Dictionary<string, object> { ["FilterPolicyScope"] = "MessageBody", ["rawmessagedelivery"] = "false" },
            topicTags: new Dictionary<string, string> { ["owner"] = "ops", ["zone"] = "blue" });

        Assert.True(TopicEntity.EntityComparer.Equals(source, same));
        Assert.Equal(TopicEntity.EntityComparer.GetHashCode(source), TopicEntity.EntityComparer.GetHashCode(same));
        Assert.False(TopicEntity.EntityComparer.Equals(source, new TopicEntity(3, "orders", true, false,
            topicAttributes: new Dictionary<string, object> { ["KmsMasterKeyId"] = "key-b", ["DisplayName"] = "Orders" },
            topicSubscriptionAttributes: source.TopicSubscriptionAttributes, topicTags: source.TopicTags)));
        Assert.False(TopicEntity.EntityComparer.Equals(source, new TopicEntity(4, "orders", true, false,
            topicAttributes: source.TopicAttributes,
            topicSubscriptionAttributes: new Dictionary<string, object> { ["RawMessageDelivery"] = "true", ["FilterPolicyScope"] = "MessageBody" },
            topicTags: source.TopicTags)));
        Assert.False(TopicEntity.EntityComparer.Equals(source, new TopicEntity(5, "orders", true, false,
            topicAttributes: source.TopicAttributes, topicSubscriptionAttributes: source.TopicSubscriptionAttributes,
            topicTags: new Dictionary<string, string> { ["zone"] = "red", ["owner"] = "ops" })));
    }

    [Fact]
    public void TopicComparer_DetectsChangedMetadataKeysWithIdenticalValues()
    {
        var source = new TopicEntity(1, "orders", true, false,
            topicAttributes: new Dictionary<string, object> { ["KmsMasterKeyId"] = "key-a" },
            topicSubscriptionAttributes: new Dictionary<string, object> { ["FilterPolicyScope"] = "MessageBody" },
            topicTags: new Dictionary<string, string> { ["zone"] = "blue" });

        Assert.False(TopicEntity.EntityComparer.Equals(source, new TopicEntity(2, "orders", true, false,
            topicAttributes: new Dictionary<string, object> { ["DisplayName"] = "key-a" },
            topicSubscriptionAttributes: source.TopicSubscriptionAttributes, topicTags: source.TopicTags)));
        Assert.False(TopicEntity.EntityComparer.Equals(source, new TopicEntity(3, "orders", true, false,
            topicAttributes: source.TopicAttributes,
            topicSubscriptionAttributes: new Dictionary<string, object> { ["FilterPolicy"] = "MessageBody" },
            topicTags: source.TopicTags)));
        Assert.False(TopicEntity.EntityComparer.Equals(source, new TopicEntity(4, "orders", true, false,
            topicAttributes: source.TopicAttributes, topicSubscriptionAttributes: source.TopicSubscriptionAttributes,
            topicTags: new Dictionary<string, string> { ["owner"] = "blue" })));
    }

    [Fact]
    public void TopicBuilder_RejectsRawDeliveryConflictWithoutLosingTheFirstDeclaration()
    {
        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        TopicHandle first = builder.CreateTopic("orders", true, false);
        TopicHandle reused = builder.CreateTopic("orders", true, false,
            topicSubscriptionAttributes: new Dictionary<string, object> { ["rawmessagedelivery"] = "true" });

        Assert.Same(first, reused);
        Assert.Throws<ArgumentException>(() => builder.CreateTopic("orders", true, false,
            topicSubscriptionAttributes: new Dictionary<string, object> { ["RawMessageDelivery"] = "false" }));

        TopicEntity topic = Assert.IsType<TopicEntity>(Assert.Single(builder.BuildTopologyLayout().Topics));
        Assert.Equal(first.Id, topic.Id);
        Assert.Equal("true", topic.TopicSubscriptionAttributes["RawMessageDelivery"]);
    }

    [Fact]
    public void TopicNameComparer_UsesAwsNameAcrossSubtypesWithoutLifecycleFlags()
    {
        var source = new TopicEntity(1, "orders", true, false);
        var changedLifetime = new TopicEntity(2, "orders", false, true);
        var renamed = new TopicEntity(3, "other", true, false);
        var subtype = new DerivedTopicEntity("orders", true, false);

        Assert.True(TopicEntity.NameComparer.Equals(source, changedLifetime));
        Assert.Equal(TopicEntity.NameComparer.GetHashCode(source), TopicEntity.NameComparer.GetHashCode(changedLifetime));
        Assert.False(TopicEntity.NameComparer.Equals(source, renamed));
        Assert.True(TopicEntity.NameComparer.Equals(source, subtype));
        Assert.Equal(TopicEntity.NameComparer.GetHashCode(source), TopicEntity.NameComparer.GetHashCode(subtype));
        Assert.True(TopicEntity.NameComparer.Equals(source, source));
        Assert.True(TopicEntity.NameComparer.Equals(null, null));
        Assert.False(TopicEntity.NameComparer.Equals(source, null));
        Assert.False(TopicEntity.NameComparer.Equals(null, source));
    }

    sealed class DerivedQueueEntity(string name, bool durable, bool autoDelete)
        : QueueEntity(0, name, durable, autoDelete);

    sealed class DerivedTopicEntity(string name, bool durable, bool autoDelete)
        : TopicEntity(0, name, durable, autoDelete);

    [Fact]
    public void QueueBuilder_RejectsSameAwsNameFromDifferentEntityTypes()
    {
        var builder = new DerivedEntityBuilder();
        builder.CreateQueue("orders", true, false);

        Assert.Throws<ArgumentException>(() => builder.AddDerivedQueue("orders"));
        Assert.Single(builder.BuildTopologyLayout().Queues);
    }

    [Fact]
    public void TopicBuilder_RejectsSameAwsNameFromDifferentEntityTypes()
    {
        var builder = new DerivedEntityBuilder();
        builder.CreateTopic("orders", true, false);

        Assert.Throws<ArgumentException>(() => builder.AddDerivedTopic("orders"));
        Assert.Single(builder.BuildTopologyLayout().Topics);
    }

    sealed class DerivedEntityBuilder : ReceiveEndpointBrokerTopologyBuilder
    {
        public QueueHandle AddDerivedQueue(string name) => Queues.GetOrAdd(new DerivedQueueEntity(name, true, false));

        public TopicHandle AddDerivedTopic(string name) => Topics.GetOrAdd(new DerivedTopicEntity(name, true, false));
    }
}
