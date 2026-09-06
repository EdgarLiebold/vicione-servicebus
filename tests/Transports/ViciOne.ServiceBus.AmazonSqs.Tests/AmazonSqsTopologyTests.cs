using System.Reflection;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsTopologyTests
{
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
            ["RawMessageDelivery"] = "false"
        };

        var topic = new TopicEntity(1, "orders", true, false, topicSubscriptionAttributes: subscriptionAttributes);

        Assert.Equal("false", topic.TopicSubscriptionAttributes["RawMessageDelivery"]);
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
