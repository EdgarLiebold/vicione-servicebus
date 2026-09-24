using System.Reflection;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "probe-reports-complete-topology")]
    public void BrokerTopologyProbe_ReportsEntityLifetimeAndEverySubscriptionPair()
    {
        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        TopicHandle orders = builder.CreateTopic("orders", true, false);
        TopicHandle alerts = builder.CreateTopic("alerts", false, true);
        QueueHandle accounting = builder.CreateQueue("accounting", true, false);
        QueueHandle fulfillment = builder.CreateQueue("fulfillment", false, true);

        builder.CreateQueueSubscription(orders, accounting);
        builder.CreateQueueSubscription(orders, fulfillment);
        builder.CreateQueueSubscription(alerts, fulfillment);

        BrokerTopology topology = builder.BuildTopologyLayout();
        IProbeResult probe = topology.GetProbeResult(TestContext.Current.CancellationToken);

        Assert.Equal(3, probe.Results.Count);
        Assert.Equal(
            [
                ("alerts", false, true),
                ("orders", true, false)
            ],
            Scopes(probe.Results, "topic")
                .Select(scope => ((string)scope["name"], (bool)scope["durable"], (bool)scope["autoDelete"]))
                .OrderBy(entity => entity.Item1, StringComparer.Ordinal));
        Assert.Equal(
            [
                ("accounting", true, false),
                ("fulfillment", false, true)
            ],
            Scopes(probe.Results, "queue")
                .Select(scope => ((string)scope["name"], (bool)scope["durable"], (bool)scope["autoDelete"]))
                .OrderBy(entity => entity.Item1, StringComparer.Ordinal));
        Assert.Equal(
            [
                ("alerts", "fulfillment"),
                ("orders", "accounting"),
                ("orders", "fulfillment")
            ],
            Scopes(probe.Results, "queueSubscription")
                .Select(scope => ((string)scope["source"], (string)scope["destination"]))
                .OrderBy(link => link.Item1, StringComparer.Ordinal)
                .ThenBy(link => link.Item2, StringComparer.Ordinal));
    }

    static IReadOnlyList<IReadOnlyDictionary<string, object>> Scopes(IReadOnlyDictionary<string, object> probe, string key)
    {
        return Assert.IsAssignableFrom<IReadOnlyList<IReadOnlyDictionary<string, object>>>(probe[key]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "one-source-can-target-multiple-queues")]
    public void QueueSubscriptions_PreserveEveryDistinctSourceAndDestinationPair()
    {
        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic("orders", true, false);
        QueueHandle accounting = builder.CreateQueue("accounting", true, false);
        QueueHandle fulfillment = builder.CreateQueue("fulfillment", true, false);

        builder.CreateQueueSubscription(topic, accounting);
        builder.CreateQueueSubscription(topic, fulfillment);

        BrokerTopology topology = builder.BuildTopologyLayout();
        Assert.Collection(
            topology.QueueSubscriptions.OrderBy(subscription => subscription.Destination.EntityName, StringComparer.Ordinal),
            subscription => Assert.Equal("accounting", subscription.Destination.EntityName),
            subscription => Assert.Equal("fulfillment", subscription.Destination.EntityName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "explicit-raw-delivery-setting-is-preserved")]
    public void Topic_PreservesAnExplicitRawMessageDeliverySetting()
    {
        var subscriptionAttributes = new Dictionary<string, object>
        {
            ["rawmessagedelivery"] = "false"
        };

        var topic = new TopicEntity(1, "orders", true, false, topicSubscriptionAttributes: subscriptionAttributes);

        KeyValuePair<string, object> attribute = Assert.Single(topic.TopicSubscriptionAttributes);
        Assert.Equal("RawMessageDelivery", attribute.Key);
        Assert.Equal("false", attribute.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-API", "broker-topology-uses-provider-native-names")]
    public void BrokerTopology_UsesProviderNativeNamesAndContainsNoUnsupportedTopicChaining()
    {
        ConstructorInfo constructor = Assert.Single(typeof(AmazonSqsBrokerTopology).GetConstructors());
        Assert.Equal("topics", constructor.GetParameters()[0].Name);
        Assert.DoesNotContain(typeof(IBrokerTopologyBuilder).GetMethods(), method => method.Name == "CreateTopicSubscription");
        Assert.DoesNotContain(typeof(BrokerTopology).GetProperties(), property => property.Name == "TopicSubscriptions");

        Assembly product = typeof(AmazonSqsBrokerTopology).Assembly;
        Assert.Null(product.GetType("ViciOne.ServiceBus.AmazonSqs.Topology.TopicSubscription"));
        Assert.Null(product.GetType("ViciOne.ServiceBus.AmazonSqs.Topology.TopicSubscriptionEntity"));
        Assert.Null(product.GetType("ViciOne.ServiceBus.AmazonSqs.Topology.TopicSubscriptionHandle"));
    }
}
