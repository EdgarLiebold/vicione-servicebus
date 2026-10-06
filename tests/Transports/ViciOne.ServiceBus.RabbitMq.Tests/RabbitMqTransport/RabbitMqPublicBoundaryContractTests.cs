using System.Collections.Generic;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqPublicBoundaryContractTests
{
    private static readonly Uri HostAddress = new("rabbitmq://broker.internal/production");

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-REDELIVERY", "published-delay-membership-is-immutable")]
    public void RedeliveryIntervals_CannotDivergeFromTheDeclaredRoutingKeys()
    {
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var endpoint = new RabbitMqEndpointConfiguration(topology);
        var settings = new RabbitMqReceiveSettings(endpoint, "orders", ExchangeType.Fanout, durable: true, autoDelete: false);
        TimeSpan[] supplied = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)];
        var plan = new RabbitMqQueueRedeliveryPlan(settings, supplied);
        supplied[0] = TimeSpan.FromSeconds(9);

        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)], plan.Intervals);
        Assert.Equal("1000", plan.GetRoutingKey(TimeSpan.FromSeconds(1)));
        Assert.Equal("5000", plan.GetRoutingKey(TimeSpan.FromSeconds(5)));

        if (plan.Intervals is IList<TimeSpan> exported)
        {
            try
            {
                exported[0] = TimeSpan.FromSeconds(2);
            }
            catch (NotSupportedException)
            {
                // A correctly read-only facade can reject the mutation.
            }
        }

        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)], plan.Intervals);
        Assert.Equal("1000", plan.GetRoutingKey(TimeSpan.FromSeconds(1)));
        Assert.Equal("5000", plan.GetRoutingKey(TimeSpan.FromSeconds(5)));
        ConfigurationException failure = Assert.Throws<ConfigurationException>(
            () => plan.GetRoutingKey(TimeSpan.FromSeconds(2)));
        Assert.Contains("was not declared", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CLUSTER-NODE", "resolver-node-snapshot-survives-caller-array-mutation")]
    public void Resolver_UsesItsConfiguredNodeSnapshotAcrossLazyEnumerationAndRotation()
    {
        RabbitMqHostSettings settings = new Uri("rabbitmq://defaults.internal:5678/").GetHostSettings();
        ClusterNode firstNode = ClusterNode.Parse("first.internal:5671");
        ClusterNode secondNode = ClusterNode.Parse("second.internal");
        ClusterNode[] supplied = [firstNode, secondNode];
        var resolver = new SequentialEndpointResolver(supplied, settings);
        IEnumerable<AmqpTcpEndpoint> firstAttempt = resolver.All();
        supplied[0] = ClusterNode.Parse("replacement-one.internal:6001");
        supplied[1] = ClusterNode.Parse("replacement-two.internal:6002");

        AmqpTcpEndpoint first = Assert.Single(firstAttempt);
        Assert.Equal("first.internal", first.HostName);
        Assert.Equal(5671, first.Port);
        Assert.Equal(firstNode, resolver.LastHost);
        AmqpTcpEndpoint second = Assert.Single(resolver.All());
        Assert.Equal("second.internal", second.HostName);
        Assert.Equal(5678, second.Port);
        Assert.Equal(secondNode, resolver.LastHost);
        AmqpTcpEndpoint third = Assert.Single(resolver.All());
        Assert.Equal("first.internal", third.HostName);
        Assert.Equal(5671, third.Port);
        Assert.Equal(firstNode, resolver.LastHost);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-ENTITY-NAME", "terminal-linefeed-is-not-a-supported-entity-character")]
    public void EntityValidation_RejectsTheEntireInvalidNameIncludingAnEncodedTerminalLinefeed()
    {
        IEntityNameValidator validator = RabbitMqEntityNameValidator.Validator;
        Assert.True(validator.IsValidEntityName("你好-Ö١._:orders"));
        Assert.True(validator.IsValidEntityName(new string('q', 255)));
        Assert.False(validator.IsValidEntityName(new string('q', 256)));
        RabbitMqAddressException lengthFailure = Assert.Throws<RabbitMqAddressException>(
            () => validator.ThrowIfInvalidEntityName(new string('q', 256)));
        Assert.Contains("255 bytes", lengthFailure.Message, StringComparison.Ordinal);

        Assert.False(validator.IsValidEntityName("orders\n"));
        RabbitMqAddressException characterFailure = Assert.Throws<RabbitMqAddressException>(
            () => validator.ThrowIfInvalidEntityName("orders\n"));
        Assert.Contains("only Unicode letters", characterFailure.Message, StringComparison.Ordinal);
        Assert.Throws<RabbitMqAddressException>(() => new RabbitMqEndpointAddress(HostAddress, new Uri("queue:orders%0A")));
        Assert.Throws<RabbitMqAddressException>(() => new RabbitMqEndpointAddress(HostAddress, "orders\n"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-SETTINGS", "single-active-consumer-survives-send-address-roundtrip")]
    public void SingleActiveConsumer_SurvivesProjectedFullAndShortAddressesIntoTheActualQueueTopology()
    {
        var settings = new RabbitMqSendSettings(new RabbitMqEndpointAddress(
            HostAddress, "orders", bindToQueue: true, queueName: "orders.queue", singleActiveConsumer: true));
        Queue initialQueue = Assert.Single(settings.GetBrokerTopology().Queues);
        Assert.Equal(true, initialQueue.QueueArguments[RabbitMQ.Client.Headers.XSingleActiveConsumer]);
        var disabled = new RabbitMqSendSettings(new RabbitMqEndpointAddress(
            HostAddress, "orders", bindToQueue: true, queueName: "orders.queue", singleActiveConsumer: false));
        Assert.False(Assert.Single(disabled.GetBrokerTopology().Queues).QueueArguments.ContainsKey(
            RabbitMQ.Client.Headers.XSingleActiveConsumer));

        RabbitMqEndpointAddress projected = settings.GetSendAddress(HostAddress);
        Assert.True(projected.SingleActiveConsumer);
        Uri fullAddress = projected;
        RabbitMqEndpointAddress[] reconstructed =
        [
            new(HostAddress, fullAddress),
            new(HostAddress, projected.ToShortAddress())
        ];
        foreach (RabbitMqEndpointAddress address in reconstructed)
        {
            Assert.True(address.SingleActiveConsumer);
            Assert.True(address.BindToQueue);
            Assert.Equal("orders.queue", address.QueueName);
            Queue queue = Assert.Single(new RabbitMqSendSettings(address).GetBrokerTopology().Queues);
            Assert.Equal("orders.queue", queue.QueueName);
            Assert.Equal(true, queue.QueueArguments[RabbitMQ.Client.Headers.XSingleActiveConsumer]);
        }
    }
}
