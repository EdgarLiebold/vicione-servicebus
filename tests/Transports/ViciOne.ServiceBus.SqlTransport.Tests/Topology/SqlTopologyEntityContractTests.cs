using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Topology;

public sealed class SqlTopologyEntityContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-TOPOLOGY-IDENTITY", "queue-identity-includes-every-broker-setting")]
    public void QueueComparer_UsesExactTypeNameIdleTimeoutAndDeliveryLimit()
    {
        var baseline = Queue();
        var equivalent = Queue(id: 2);

        Assert.True(QueueEntity.QueueComparer.Equals(baseline, baseline));
        Assert.True(QueueEntity.QueueComparer.Equals(baseline, equivalent));
        Assert.Equal(QueueEntity.QueueComparer.GetHashCode(baseline), QueueEntity.QueueComparer.GetHashCode(equivalent));
        Assert.False(QueueEntity.QueueComparer.Equals(baseline, null));
        Assert.False(QueueEntity.QueueComparer.Equals(null, baseline));
        Assert.True(QueueEntity.QueueComparer.Equals(null, null));
        Assert.False(QueueEntity.QueueComparer.Equals(baseline, new DerivedQueueEntity()));
        Assert.False(QueueEntity.QueueComparer.Equals(baseline, Queue(name: "priority")));
        Assert.False(QueueEntity.QueueComparer.Equals(baseline, Queue(autoDeleteOnIdle: TimeSpan.FromMinutes(10))));
        Assert.False(QueueEntity.QueueComparer.Equals(baseline, Queue(maxDeliveryCount: 8)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-TOPOLOGY-IDENTITY", "entity-name-comparers-are-exact-and-type-safe")]
    public void NameComparers_UseExactTypeAndNameOnly()
    {
        var queue = Queue();
        var sameQueueName = new QueueEntity(2, "orders", null, null);
        var topic = Topic();
        var sameTopicName = Topic(id: 2);

        Assert.True(QueueEntity.NameComparer.Equals(queue, sameQueueName));
        Assert.Equal(QueueEntity.NameComparer.GetHashCode(queue), QueueEntity.NameComparer.GetHashCode(sameQueueName));
        Assert.False(QueueEntity.NameComparer.Equals(queue, Queue(name: "priority")));
        Assert.False(QueueEntity.NameComparer.Equals(queue, Queue(name: "Orders")));
        Assert.False(QueueEntity.NameComparer.Equals(queue, new DerivedQueueEntity()));
        Assert.True(TopicEntity.NameComparer.Equals(topic, sameTopicName));
        Assert.Equal(TopicEntity.NameComparer.GetHashCode(topic), TopicEntity.NameComparer.GetHashCode(sameTopicName));
        Assert.False(TopicEntity.NameComparer.Equals(topic, Topic(name: "audit")));
        Assert.False(TopicEntity.NameComparer.Equals(topic, Topic(name: "Events")));
        Assert.False(TopicEntity.NameComparer.Equals(topic, new DerivedTopicEntity()));
        Assert.False(TopicEntity.NameComparer.Equals(topic, null));
        Assert.False(TopicEntity.NameComparer.Equals(null, topic));
        Assert.True(TopicEntity.NameComparer.Equals(null, null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-TOPOLOGY-IDENTITY", "queue-subscription-uses-logical-entity-identity")]
    public void QueueSubscriptionComparer_UsesLogicalEntitiesTypeAndRoutingContract()
    {
        var baseline = QueueSubscription();
        var equivalent = QueueSubscription(id: 2);

        Assert.True(QueueSubscriptionEntity.EntityComparer.Equals(baseline, baseline));
        Assert.True(QueueSubscriptionEntity.EntityComparer.Equals(baseline, equivalent));
        Assert.Equal(
            QueueSubscriptionEntity.EntityComparer.GetHashCode(baseline),
            QueueSubscriptionEntity.EntityComparer.GetHashCode(equivalent));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(baseline, null));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(null, baseline));
        Assert.True(QueueSubscriptionEntity.EntityComparer.Equals(null, null));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(baseline, new DerivedQueueSubscriptionEntity()));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(baseline, QueueSubscription(topicName: "audit")));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(baseline, QueueSubscription(queueName: "priority")));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(
            baseline,
            QueueSubscription(autoDeleteOnIdle: TimeSpan.FromMinutes(10))));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(baseline, QueueSubscription(maxDeliveryCount: 8)));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(baseline, QueueSubscription(type: SqlSubscriptionType.Pattern)));
        Assert.False(QueueSubscriptionEntity.EntityComparer.Equals(baseline, QueueSubscription(routingKey: "orders.*")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-TOPOLOGY-IDENTITY", "topic-subscription-uses-logical-entity-identity")]
    public void TopicSubscriptionComparer_UsesLogicalEntitiesTypeAndRoutingContract()
    {
        var baseline = TopicSubscription();
        var equivalent = TopicSubscription(id: 2);

        Assert.True(TopicSubscriptionEntity.EntityComparer.Equals(baseline, baseline));
        Assert.True(TopicSubscriptionEntity.EntityComparer.Equals(baseline, equivalent));
        Assert.Equal(
            TopicSubscriptionEntity.EntityComparer.GetHashCode(baseline),
            TopicSubscriptionEntity.EntityComparer.GetHashCode(equivalent));
        Assert.False(TopicSubscriptionEntity.EntityComparer.Equals(baseline, null));
        Assert.False(TopicSubscriptionEntity.EntityComparer.Equals(null, baseline));
        Assert.True(TopicSubscriptionEntity.EntityComparer.Equals(null, null));
        Assert.False(TopicSubscriptionEntity.EntityComparer.Equals(baseline, new DerivedTopicSubscriptionEntity()));
        Assert.False(TopicSubscriptionEntity.EntityComparer.Equals(baseline, TopicSubscription(sourceName: "audit")));
        Assert.False(TopicSubscriptionEntity.EntityComparer.Equals(baseline, TopicSubscription(destinationName: "archive")));
        Assert.False(TopicSubscriptionEntity.EntityComparer.Equals(baseline, TopicSubscription(type: SqlSubscriptionType.Pattern)));
        Assert.False(TopicSubscriptionEntity.EntityComparer.Equals(baseline, TopicSubscription(routingKey: "orders.*")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-TOPOLOGY-DIAGNOSTICS", "all-entities-and-broker-settings-are-projected")]
    public void BrokerProbe_ProjectsEveryEntityAndBrokerSetting()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic("source");
        TopicHandle destination = builder.CreateTopic("destination");
        QueueHandle queue = builder.CreateQueue("orders", TimeSpan.FromMinutes(5), maxDeliveryCount: 7);
        builder.CreateTopicSubscription(source, destination, SqlSubscriptionType.All, null);
        builder.CreateQueueSubscription(source, queue, SqlSubscriptionType.RoutingKey, "orders.created");

        IProbeResult probe = builder.BuildBrokerTopology().GetProbeResult(TestContext.Current.CancellationToken);

        IReadOnlyList<IReadOnlyDictionary<string, object>> topics = Scopes(probe.Results, "topic");
        Assert.All(topics, topic => Assert.Equal(["name"], topic.Keys));
        Assert.Equal(["destination", "source"], topics.Select(Value<string>("name")).Order());
        IReadOnlyDictionary<string, object> queueScope = Assert.Single(Scopes(probe.Results, "queue"));
        Assert.Equal(["autoDeleteOnIdle", "maxDeliveryCount", "name"], queueScope.Keys.Order());
        Assert.Equal("orders", Value<string>("name")(queueScope));
        Assert.Equal(TimeSpan.FromMinutes(5), Value<TimeSpan>("autoDeleteOnIdle")(queueScope));
        Assert.Equal(7, Value<int>("maxDeliveryCount")(queueScope));

        IReadOnlyDictionary<string, object> topicSubscription = Assert.Single(Scopes(probe.Results, "topic-subscription"));
        Assert.Equal(["destination", "source", "subscriptionType"], topicSubscription.Keys.Order());
        Assert.Equal("source", Value<string>("source")(topicSubscription));
        Assert.Equal("destination", Value<string>("destination")(topicSubscription));
        Assert.Equal(SqlSubscriptionType.All, Value<SqlSubscriptionType>("subscriptionType")(topicSubscription));

        IReadOnlyDictionary<string, object> queueSubscription = Assert.Single(Scopes(probe.Results, "queue-subscription"));
        Assert.Equal(["destination", "routingKey", "source", "subscriptionType"], queueSubscription.Keys.Order());
        Assert.Equal("source", Value<string>("source")(queueSubscription));
        Assert.Equal("orders", Value<string>("destination")(queueSubscription));
        Assert.Equal(SqlSubscriptionType.RoutingKey, Value<SqlSubscriptionType>("subscriptionType")(queueSubscription));
        Assert.Equal("orders.created", Value<string>("routingKey")(queueSubscription));
    }

    private static Func<IReadOnlyDictionary<string, object>, T> Value<T>(string key) =>
        scope => Assert.IsType<T>(Assert.Contains(key, scope));

    private static IReadOnlyList<IReadOnlyDictionary<string, object>> Scopes(
        IReadOnlyDictionary<string, object> parent,
        string key) =>
        Assert.Contains(key, parent) switch
        {
            IReadOnlyDictionary<string, object> single => [single],
            IReadOnlyList<IReadOnlyDictionary<string, object>> multiple => multiple,
            object value => throw new Xunit.Sdk.XunitException($"Unexpected probe value '{value.GetType()}' for '{key}'."),
        };

    private static QueueEntity Queue(
        long id = 1,
        string name = "orders",
        TimeSpan? autoDeleteOnIdle = default,
        int? maxDeliveryCount = 7) =>
        new(id, name, autoDeleteOnIdle ?? TimeSpan.FromMinutes(5), maxDeliveryCount);

    private static TopicEntity Topic(long id = 1, string name = "events") => new(id, name);

    private static QueueSubscriptionEntity QueueSubscription(
        long id = 1,
        string topicName = "events",
        string queueName = "orders",
        TimeSpan? autoDeleteOnIdle = default,
        int? maxDeliveryCount = 7,
        SqlSubscriptionType type = SqlSubscriptionType.RoutingKey,
        string? routingKey = "orders.created") =>
        new(id, Topic(name: topicName), Queue(name: queueName, autoDeleteOnIdle: autoDeleteOnIdle, maxDeliveryCount: maxDeliveryCount), type,
            routingKey);

    private static TopicSubscriptionEntity TopicSubscription(
        long id = 1,
        string sourceName = "events",
        string destinationName = "audit",
        SqlSubscriptionType type = SqlSubscriptionType.RoutingKey,
        string? routingKey = "orders.created") =>
        new(id, Topic(name: sourceName), Topic(name: destinationName), type, routingKey);

    private sealed class DerivedQueueEntity() : QueueEntity(1, "orders", TimeSpan.FromMinutes(5), 7);
    private sealed class DerivedTopicEntity() : TopicEntity(1, "events");
    private sealed class DerivedQueueSubscriptionEntity()
        : QueueSubscriptionEntity(1, Topic(), Queue(), SqlSubscriptionType.RoutingKey, "orders.created");
    private sealed class DerivedTopicSubscriptionEntity()
        : TopicSubscriptionEntity(1, Topic(), Topic(name: "audit"), SqlSubscriptionType.RoutingKey, "orders.created");
}
