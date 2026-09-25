using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Topology;

public sealed class RabbitMqMessagePublishTopologyTests
{
    [Theory]
    [InlineData(RabbitMQ.Client.ExchangeType.Topic, "#")]
    [InlineData(RabbitMQ.Client.ExchangeType.Fanout, "")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-PUBLISH-TOPOLOGY", "parent-exchange-routing-key")]
    public void ApplyingMessageTopology_BindsParentWithItsRequiredRoutingKey(string parentType, string expectedRoutingKey)
    {
        RabbitMqMessagePublishTopology<PublishTopologyMessage> messageTopology = CreateMessageTopology();
        var builder = new PublishEndpointBrokerTopologyBuilder();
        ExchangeHandle parent = builder.ExchangeDeclare("all-events", parentType, true, false, new Dictionary<string, object?>());
        builder.Exchange = parent;

        messageTopology.Apply(builder);

        BrokerTopology snapshot = builder.BuildBrokerTopology();
        ExchangeToExchangeBinding binding = Assert.Single(snapshot.ExchangeBindings);
        Assert.Equal("all-events", binding.Source.ExchangeName);
        Assert.Equal(messageTopology.Exchange.ExchangeName, binding.Destination.ExchangeName);
        Assert.Equal(expectedRoutingKey, binding.RoutingKey);
        Assert.Empty(binding.Arguments);
        Assert.Same(parent, builder.Exchange);
        Assert.Equal(2, snapshot.Exchanges.Length);
        Assert.Empty(snapshot.Queues);
        Assert.Empty(snapshot.QueueBindings);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-PUBLISH-TOPOLOGY", "excluded-message-preserves-existing-builder")]
    public void ExcludedMessage_LeavesParentTopologyUntouched()
    {
        var publishTopology = new RabbitMqPublishTopology(RabbitMqBusFactory.CreateMessageTopology());
        var messageTopology = Assert.IsType<RabbitMqMessagePublishTopology<ConcretePublishEvent>>(
            ((IRabbitMqPublishTopologyConfigurator)publishTopology).GetMessageTopology<ConcretePublishEvent>());
        messageTopology.BindQueue("ignored", "ignored-queue", null);
        ((IRabbitMqMessagePublishTopologyConfigurator)messageTopology).Exclude = true;
        var builder = new PublishEndpointBrokerTopologyBuilder();
        ExchangeHandle parent = builder.ExchangeDeclare("all-events", RabbitMQ.Client.ExchangeType.Topic,
            true, false, new Dictionary<string, object?>());
        builder.Exchange = parent;

        messageTopology.Apply(builder);

        BrokerTopology snapshot = builder.BuildBrokerTopology();
        Assert.Same(parent, builder.Exchange);
        Assert.Equal("all-events", Assert.Single(snapshot.Exchanges).ExchangeName);
        Assert.Empty(snapshot.ExchangeBindings);
        Assert.Empty(snapshot.Queues);
        Assert.Empty(snapshot.QueueBindings);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-PUBLISH-TOPOLOGY", "alternate-exchange-queue-route")]
    public void AlternateExchangeQueue_DeclaresAnUnroutableMessageRoute()
    {
        RabbitMqMessagePublishTopology<PublishTopologyMessage> messageTopology = CreateMessageTopology();
        messageTopology.BindAlternateExchangeQueue("unroutable", "unroutable-queue", null);

        BrokerTopology snapshot = messageTopology.GetBrokerTopology();

        Exchange messageExchange = Assert.Single(snapshot.Exchanges,
            exchange => exchange.ExchangeName == messageTopology.Exchange.ExchangeName);
        Assert.Equal("unroutable", messageExchange.ExchangeArguments[RabbitMQ.Client.Headers.AlternateExchange]);
        Exchange alternateExchange = Assert.Single(snapshot.Exchanges,
            exchange => exchange.ExchangeName == "unroutable");
        Assert.True(alternateExchange.Durable);
        Assert.False(alternateExchange.AutoDelete);
        Queue queue = Assert.Single(snapshot.Queues);
        Assert.Equal("unroutable-queue", queue.QueueName);
        Assert.True(queue.Durable);
        ExchangeToQueueBinding binding = Assert.Single(snapshot.QueueBindings);
        Assert.Equal("unroutable", binding.Source.ExchangeName);
        Assert.Equal("unroutable-queue", binding.Destination.QueueName);
        Assert.Empty(snapshot.ExchangeBindings);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-PUBLISH-TOPOLOGY", "direct-implemented-contract-hierarchy")]
    public void MaintainedHierarchy_RoutesThroughEachDirectImplementedContract()
    {
        var publishTopology = new RabbitMqPublishTopology(RabbitMqBusFactory.CreateMessageTopology())
        {
            BrokerTopologyOptions = PublishBrokerTopologyOptions.MaintainHierarchy,
        };
        var configurator = (IRabbitMqPublishTopologyConfigurator)publishTopology;
        var concrete = Assert.IsType<RabbitMqMessagePublishTopology<ConcretePublishEvent>>(
            configurator.GetMessageTopology<ConcretePublishEvent>());
        var derived = Assert.IsType<RabbitMqMessagePublishTopology<IDerivedPublishEvent>>(
            configurator.GetMessageTopology<IDerivedPublishEvent>());
        var root = Assert.IsType<RabbitMqMessagePublishTopology<IRootPublishEvent>>(
            configurator.GetMessageTopology<IRootPublishEvent>());

        BrokerTopology snapshot = concrete.GetBrokerTopology();

        Assert.Equal(3, snapshot.Exchanges.Length);
        Assert.Equal(2, snapshot.ExchangeBindings.Length);
        Assert.Contains(snapshot.ExchangeBindings, binding =>
            binding.Source.ExchangeName == concrete.Exchange.ExchangeName
            && binding.Destination.ExchangeName == derived.Exchange.ExchangeName
            && binding.RoutingKey == "");
        Assert.Contains(snapshot.ExchangeBindings, binding =>
            binding.Source.ExchangeName == derived.Exchange.ExchangeName
            && binding.Destination.ExchangeName == root.Exchange.ExchangeName
            && binding.RoutingKey == "");
        Assert.DoesNotContain(snapshot.ExchangeBindings, binding =>
            binding.Source.ExchangeName == concrete.Exchange.ExchangeName
            && binding.Destination.ExchangeName == root.Exchange.ExchangeName);
        Assert.Empty(snapshot.Queues);
    }

    private static RabbitMqMessagePublishTopology<PublishTopologyMessage> CreateMessageTopology()
    {
        var publishTopology = new RabbitMqPublishTopology(RabbitMqBusFactory.CreateMessageTopology());
        return Assert.IsType<RabbitMqMessagePublishTopology<PublishTopologyMessage>>(
            ((IRabbitMqPublishTopologyConfigurator)publishTopology).GetMessageTopology<PublishTopologyMessage>());
    }
}

public sealed class PublishTopologyMessage;

public interface IRootPublishEvent;

public interface IDerivedPublishEvent : IRootPublishEvent;

public sealed class ConcretePublishEvent : IDerivedPublishEvent;
