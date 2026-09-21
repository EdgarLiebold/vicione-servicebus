using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Topology;

public sealed class RabbitMqSendSettingsContractTests
{
    private static readonly Uri HostAddress = new("rabbitmq://localhost/production");

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-SETTINGS", "minimal-and-complete-diagnostic-projection")]
    public void ToString_DistinguishesMinimalAndFullyConfiguredSettings()
    {
        var minimal = new RabbitMqSendSettings(new RabbitMqEndpointAddress(HostAddress, "minimal"));
        var complete = new RabbitMqSendSettings(new RabbitMqEndpointAddress(
            HostAddress,
            "orders",
            exchangeType: ExchangeType.Direct,
            durable: true,
            autoDelete: true,
            bindToQueue: true,
            queueName: "orders.queue",
            alternateExchange: "fallback",
            singleActiveConsumer: true));
        complete.SetQueueArgument("x-max-length", 100);
        complete.SetQueueArgument("removed", "value");
        complete.SetQueueArgument("removed", null);

        Assert.Equal("durable", minimal.ToString());
        Assert.Equal(
            "durable, auto-delete, direct, bind->orders.queue, e:alternate-exchange=fallback, " +
            "q:x-single-active-consumer=True, q:x-max-length=100",
            complete.ToString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-SETTINGS", "address-and-topology-preserve-every-setting")]
    public void AddressAndTopology_PreserveDelayedBindingsAndQueueArguments()
    {
        var settings = new RabbitMqSendSettings(new RabbitMqEndpointAddress(
            HostAddress,
            "orders",
            durable: true,
            bindToQueue: true,
            queueName: "orders.queue",
            delayedType: ExchangeType.Topic,
            bindExchanges: ["source.one", "source.two"],
            alternateExchange: "fallback",
            singleActiveConsumer: true));

        RabbitMqEndpointAddress address = settings.GetSendAddress(HostAddress);
        BrokerTopology topology = settings.GetBrokerTopology();

        Assert.Equal(RabbitMqEndpointAddress.DelayedMessageExchangeType, address.ExchangeType);
        Assert.Equal(ExchangeType.Topic, address.DelayedType);
        Assert.Equal(["source.one", "source.two"], address.BindExchanges);
        Assert.Equal("fallback", address.AlternateExchange);
        Assert.True(address.BindToQueue);
        Assert.Equal("orders.queue", address.QueueName);
        Assert.Equal(3, topology.Exchanges.Length);
        Assert.Equal(2, topology.ExchangeBindings.Length);
        Queue queue = Assert.Single(topology.Queues);
        Assert.Equal("orders.queue", queue.QueueName);
        Assert.Equal(true, queue.QueueArguments[RabbitMQ.Client.Headers.XSingleActiveConsumer]);
        ExchangeToQueueBinding binding = Assert.Single(topology.QueueBindings);
        Assert.Equal("orders", binding.Source.ExchangeName);
        Assert.Equal("orders.queue", binding.Destination.QueueName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-SEND-SETTINGS", "direct-reply-to-declares-no-topology")]
    public void DirectReplyTo_ProducesAnEmptyBrokerTopology()
    {
        var settings = new RabbitMqSendSettings(new RabbitMqEndpointAddress(
            HostAddress,
            RabbitMqExchangeNames.ReplyTo,
            bindToQueue: true));

        BrokerTopology topology = settings.GetBrokerTopology();

        Assert.Empty(topology.Exchanges);
        Assert.Empty(topology.Queues);
        Assert.Empty(topology.ExchangeBindings);
        Assert.Empty(topology.QueueBindings);
    }
}
