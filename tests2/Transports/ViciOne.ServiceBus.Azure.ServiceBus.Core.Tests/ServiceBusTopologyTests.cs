using ViciOne.ServiceBus.AzureServiceBusTransport;
using ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;
using ViciOne.ServiceBus.AzureServiceBusTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests;

public sealed class ServiceBusTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "consume-subscription-is-exact-and-not-expanded-to-base-interface")]
    public void ConsumeSubscription_BindsOnlyTheExplicitInterfaceToTheQueue()
    {
        var formatter = new ServiceBusMessageNameFormatter();
        IServiceBusConsumeTopologyConfigurator topology = new ServiceBusConsumeTopology(
            AzureBusFactory.CreateMessageTopology(),
            new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology()));
        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        const string queueName = "input-queue";
        builder.Queue = builder.CreateQueue(new ServiceBusQueueConfigurator(queueName).GetCreateQueueOptions());

        topology.GetMessageTopology<ISecond>().Subscribe("explicit-second");
        topology.Apply(builder);
        BrokerTopology actual = builder.BuildBrokerTopology();

        string first = formatter.GetMessageName(typeof(IFirst)).ToString();
        string second = formatter.GetMessageName(typeof(ISecond)).ToString();

        Assert.Equal([second], actual.Topics.Select(x => x.CreateTopicOptions.Name).Order().ToArray());
        var subscription = Assert.Single(actual.QueueSubscriptions);
        Assert.Equal(second, subscription.Source.CreateTopicOptions.Name);
        Assert.Equal(queueName, subscription.Destination.CreateQueueOptions.Name);
        Assert.DoesNotContain(actual.Topics, x => x.CreateTopicOptions.Name == first);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "publish-hierarchy-has-complete-exact-chain")]
    public void PublishHierarchy_ContainsEveryTopicAndOnlyTheDirectInheritanceEdges()
    {
        var formatter = new ServiceBusMessageNameFormatter();
        IServiceBusPublishTopologyConfigurator topology = new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology());
        var builder = new PublishEndpointBrokerTopologyBuilder(topology);

        topology.GetMessageTopology<IThird>().Apply(builder);
        BrokerTopology actual = builder.BuildBrokerTopology();

        string first = formatter.GetMessageName(typeof(IFirst)).ToString();
        string second = formatter.GetMessageName(typeof(ISecond)).ToString();
        string third = formatter.GetMessageName(typeof(IThird)).ToString();

        Assert.Equal([first, second, third], actual.Topics.Select(x => x.CreateTopicOptions.Name).Order().ToArray());
        Assert.Equal(2, actual.TopicSubscriptions.Length);
        Assert.Contains(actual.TopicSubscriptions,
            x => x.Source.CreateTopicOptions.Name == second && x.Destination.CreateTopicOptions.Name == first);
        Assert.Contains(actual.TopicSubscriptions,
            x => x.Source.CreateTopicOptions.Name == third && x.Destination.CreateTopicOptions.Name == second);
        Assert.DoesNotContain(actual.TopicSubscriptions,
            x => x.Source.CreateTopicOptions.Name == third && x.Destination.CreateTopicOptions.Name == first);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "single-interface-has-no-spurious-edge")]
    public void SingleInterface_CreatesOneTopicAndNoSubscription()
    {
        var formatter = new ServiceBusMessageNameFormatter();
        IServiceBusPublishTopologyConfigurator topology = new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology());
        var builder = new PublishEndpointBrokerTopologyBuilder(topology);

        topology.GetMessageTopology<ISingle>().Apply(builder);
        BrokerTopology actual = builder.BuildBrokerTopology();

        Assert.Equal(formatter.GetMessageName(typeof(ISingle)).ToString(), Assert.Single(actual.Topics).CreateTopicOptions.Name);
        Assert.Empty(actual.TopicSubscriptions);
    }

    public interface ISingle;

    public interface IFirst
    {
        string Value { get; }
    }

    public interface ISecond : IFirst;

    public interface IThird : ISecond;
}
