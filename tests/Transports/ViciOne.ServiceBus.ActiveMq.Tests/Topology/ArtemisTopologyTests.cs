using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.Topology;

public sealed class ArtemisTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "artemis-topic-subscription-is-not-an-anycast-queue")]
    public void ArtemisCompatibility_UsesANamedSharedTopicSubscriptionWithoutAnAnycastQueue()
    {
        const string topicName = "VirtualTopic.orders";
        const string endpointName = "order-service";
        var formatter = new ArtemisConsumerEndpointQueueNameFormatter();
        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        builder.Queue = builder.CreateQueue(endpointName, durable: true, autoDelete: false);
        var specification = new ConsumerConsumeTopologySpecification(topicName, formatter)
        {
            Selector = "priority = 7",
        };

        specification.Apply(builder);
        BrokerTopology topology = builder.BuildTopologyLayout();

        Assert.Equal($"Consumer.{endpointName}.{topicName}", formatter.Format(topicName, endpointName));
        ViciOne.ServiceBus.ActiveMq.Topology.Queue queue = Assert.Single(topology.Queues);
        Assert.Equal(endpointName, queue.EntityName);
        ViciOne.ServiceBus.ActiveMq.Topology.Consumer consumer = Assert.Single(topology.Consumers);
        Assert.Equal(topicName, consumer.Source.EntityName);
        Assert.Null(consumer.Destination);
        Assert.Equal($"Consumer.{endpointName}.{topicName}", consumer.ConsumerName);
        Assert.True(consumer.IsShared);

        IProbeResult probe = topology.GetProbeResult(TestContext.Current.CancellationToken);
        IDictionary<string, object> consumerProbe = Assert.IsAssignableFrom<IDictionary<string, object>>(
            Assert.Contains("consumer", probe.Results));
        Assert.Equal(topicName, Assert.Contains("source", consumerProbe));
        Assert.DoesNotContain("destination", consumerProbe);
        Assert.Equal("priority = 7", Assert.Contains("selector", consumerProbe));
        Assert.DoesNotContain("routingKey", consumerProbe);
        Assert.Equal(consumer.ConsumerName, Assert.Contains("consumerName", consumerProbe));
        Assert.Equal(true, Assert.Contains("isShared", consumerProbe));
    }
}
