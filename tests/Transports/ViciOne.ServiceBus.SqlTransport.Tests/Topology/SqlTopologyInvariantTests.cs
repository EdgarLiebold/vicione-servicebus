using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Topology;

public sealed class SqlTopologyInvariantTests
{
    private static readonly Uri HostAddress = new("db://localhost/transport");

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-TOPIC-ADDRESS", "topic-send-settings-preserve-entity-kind")]
    public void TopicSendSettings_ProduceATopicAddress()
    {
        var source = new SqlEndpointAddress(
            HostAddress,
            "events",
            type: SqlEndpointAddress.AddressType.Topic);
        var settings = new TopicSendSettings(source);

        var actual = settings.GetSendAddress(HostAddress);

        Assert.Equal(SqlEndpointAddress.AddressType.Topic, actual.Type);
        Assert.Equal(new Uri("db://localhost/transport/events?type=topic"), (Uri)actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-TOPIC-ADDRESS", "publish-topology-uses-topic-settings")]
    public void PublishTopology_ProducesTopicSendSettings()
    {
        ISqlTopologyConfiguration configuration = new SqlTopologyConfiguration(SqlBusFactory.CreateMessageTopology());

        SendSettings settings = configuration.Publish.GetMessageTopology<Event>().GetSendSettings(HostAddress);

        Assert.IsType<TopicSendSettings>(settings);
        Assert.Equal(SqlEndpointAddress.AddressType.Topic, settings.GetSendAddress(HostAddress).Type);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-TOPOLOGY-IDENTITY", "topic-subscription-destination-is-part-of-identity")]
    public void BrokerTopology_PreservesSubscriptionsToDifferentDestinationTopics()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic("source");
        TopicHandle firstDestination = builder.CreateTopic("first");
        TopicHandle secondDestination = builder.CreateTopic("second");

        builder.CreateTopicSubscription(source, firstDestination, SqlSubscriptionType.All, null);
        builder.CreateTopicSubscription(source, secondDestination, SqlSubscriptionType.All, null);

        BrokerTopology topology = builder.BuildBrokerTopology();

        Assert.Equal(2, topology.TopicSubscriptions.Length);
        Assert.Contains(topology.TopicSubscriptions, subscription => subscription.Destination.TopicName == "first");
        Assert.Contains(topology.TopicSubscriptions, subscription => subscription.Destination.TopicName == "second");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-TOPOLOGY-IDENTITY", "queue-delivery-limit-is-part-of-identity")]
    public void BrokerTopology_RejectsConflictingQueueDeliveryLimits()
    {
        var builder = new ReceiveEndpointBrokerTopologyBuilder(new TestReceiveSettings("input", maxDeliveryCount: 3));

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.CreateQueue("input", maxDeliveryCount: 7));

        Assert.Contains("settings did not match", exception.Message, StringComparison.Ordinal);
    }

    private sealed record Event;

    private sealed class TestReceiveSettings(string queueName, int? maxDeliveryCount) : ReceiveSettings
    {
        public string QueueName { get; } = queueName;
        public TimeSpan? AutoDeleteOnIdle => null;
        public int? MaxDeliveryCount { get; } = maxDeliveryCount;
        public long? QueueId => null;
        public int PrefetchCount => 1;
        public int ConcurrentMessageLimit => 1;
        public int ConcurrentDeliveryLimit => 1;
        public SqlReceiveMode ReceiveMode => SqlReceiveMode.Normal;
        public bool PurgeOnStartup => false;
        public TimeSpan LockDuration => TimeSpan.FromMinutes(1);
        public TimeSpan PollingInterval => TimeSpan.FromSeconds(1);
        public TimeSpan? UnlockDelay => null;
        public TimeSpan MaxLockDuration => TimeSpan.FromHours(1);
        public string EntityName => QueueName;
        public int MaintenanceBatchSize => 100;
        public bool DeadLetterExpiredMessages => false;
    }
}
