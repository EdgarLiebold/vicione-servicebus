using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Topology;

public sealed class RabbitMqNestedConsumeBindingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "nested-consume-bindings-preserve-direction-settings-and-sibling-parent")]
    public void Apply_BuildsTheExactNestedTreeAndRestoresTheParentForSiblings()
    {
        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        builder.Exchange = builder.ExchangeDeclare("endpoint", "fanout", true, false, new Dictionary<string, object?>());
        var root = new ExchangeBindingConsumeTopologySpecification("root", "direct", false, true)
        {
            RoutingKey = "root.key",
        };
        root.SetExchangeArgument("alternate-exchange", "root-alternate");
        root.SetBindingArgument("root-argument", 11);
        root.Bind("child", child =>
        {
            child.ExchangeType = "topic";
            child.Durable = true;
            child.AutoDelete = false;
            child.RoutingKey = "child.#";
            child.SetExchangeArgument("alternate-exchange", "child-alternate");
            child.SetBindingArgument("child-argument", 22);
            child.Bind("grandchild", grandchild =>
            {
                grandchild.RoutingKey = "grandchild.*";
                grandchild.SetBindingArgument("grandchild-argument", 33);
            });
            child.Bind("child-sibling", null);
        });
        root.Bind("root-sibling", null);

        root.Apply(builder);

        BrokerTopology topology = builder.BuildBrokerTopology();
        Assert.Empty(topology.Queues);
        Assert.Empty(topology.QueueBindings);
        Assert.Equal(6, topology.Exchanges.Length);
        Assert.Equal(5, topology.ExchangeBindings.Length);
        AssertExchange(topology, "endpoint", "fanout", true, false, null);
        AssertExchange(topology, "root", "direct", false, true, "root-alternate");
        AssertExchange(topology, "child", "topic", true, false, "child-alternate");
        AssertExchange(topology, "grandchild", "topic", true, false, null);
        AssertExchange(topology, "child-sibling", "topic", true, false, null);
        AssertExchange(topology, "root-sibling", "direct", false, true, null);
        AssertBinding(topology, "root", "endpoint", "root.key", "root-argument", 11);
        AssertBinding(topology, "child", "root", "child.#", "child-argument", 22);
        AssertBinding(topology, "grandchild", "child", "grandchild.*", "grandchild-argument", 33);
        AssertBinding(topology, "child-sibling", "child", "child.#", null, null);
        AssertBinding(topology, "root-sibling", "root", "root.key", null, null);
        Assert.Same(Assert.Single(topology.Exchanges, exchange => exchange.ExchangeName == "root"), builder.BoundExchange);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "nested-binding-requires-parent-before-declaration")]
    public void Apply_WithoutAParent_RejectsTheBindingWithoutDeclaringItsExchange()
    {
        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        builder.Exchange = builder.ExchangeDeclare("endpoint", "fanout", true, false, new Dictionary<string, object?>());
        var nested = new ExchangeToExchangeBindingConsumeTopologySpecification("orphan", "direct");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => nested.Apply(builder));

        Assert.Equal("builder", exception.ParamName);
        Assert.Null(builder.BoundExchange);
        BrokerTopology topology = builder.BuildBrokerTopology();
        Assert.Equal("endpoint", Assert.Single(topology.Exchanges).ExchangeName);
        Assert.Empty(topology.ExchangeBindings);
        Assert.Empty(topology.Queues);
        Assert.Empty(topology.QueueBindings);
    }

    private static void AssertExchange(BrokerTopology topology, string name, string type, bool durable, bool autoDelete,
        string? alternate)
    {
        Exchange exchange = Assert.Single(topology.Exchanges, exchange => exchange.ExchangeName == name);
        Assert.Equal(type, exchange.ExchangeType);
        Assert.Equal(durable, exchange.Durable);
        Assert.Equal(autoDelete, exchange.AutoDelete);
        if (alternate is null)
            Assert.Empty(exchange.ExchangeArguments);
        else
        {
            KeyValuePair<string, object?> argument = Assert.Single(exchange.ExchangeArguments);
            Assert.Equal("alternate-exchange", argument.Key);
            Assert.Equal(alternate, Assert.IsType<string>(argument.Value));
        }
    }

    private static void AssertBinding(BrokerTopology topology, string source, string destination, string routingKey,
        string? argumentName, int? argumentValue)
    {
        ExchangeToExchangeBinding binding = Assert.Single(topology.ExchangeBindings, binding => binding.Source.ExchangeName == source);
        Assert.Equal(destination, binding.Destination.ExchangeName);
        Assert.Equal(routingKey, binding.RoutingKey);
        if (argumentName is null)
            Assert.Empty(binding.Arguments);
        else
        {
            KeyValuePair<string, object?> argument = Assert.Single(binding.Arguments);
            Assert.Equal(argumentName, argument.Key);
            Assert.Equal(argumentValue, Assert.IsType<int>(argument.Value));
        }
    }
}
