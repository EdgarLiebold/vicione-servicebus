using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Fabric;

public sealed class MessageFabricTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "acyclic-exchange-and-queue-graph")]
    public async Task AcyclicBindingGraph_ConnectsEveryDeclaredDestination()
    {
        var fabric = new MessageFabric<object, FabricMessage>();
        var context = new object();

        try
        {
            fabric.ExchangeBind(context, "Namespace.A", "input-exchange", null!);
            fabric.ExchangeBind(context, "Namespace.B", "input-exchange", null!);
            fabric.QueueBind(context, "input-exchange", "input-queue");

            IMessageExchange<FabricMessage> namespaceA = fabric.GetExchange(context, "Namespace.A", ExchangeType.FanOut);
            IMessageExchange<FabricMessage> namespaceB = fabric.GetExchange(context, "Namespace.B", ExchangeType.FanOut);
            IMessageExchange<FabricMessage> inputExchange = fabric.GetExchange(context, "input-exchange", ExchangeType.FanOut);
            IMessageQueue<object, FabricMessage> inputQueue = fabric.GetQueue(context, "input-queue");

            Assert.Same(inputExchange, Assert.Single(namespaceA.Sinks));
            Assert.Same(inputExchange, Assert.Single(namespaceB.Sinks));
            Assert.Same(inputQueue, Assert.Single(inputExchange.Sinks));
            Assert.Equal("input-queue", inputQueue.Name);
        }
        finally
        {
            await fabric.Stop(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "cycle-rejected-without-partial-edge")]
    public async Task CyclicBinding_IsRejectedWithoutChangingTheGraph()
    {
        var fabric = new MessageFabric<object, FabricMessage>();
        var context = new object();

        try
        {
            fabric.ExchangeBind(context, "Namespace.A", "input-exchange", null!);
            fabric.ExchangeBind(context, "Namespace.B", "input-exchange", null!);
            fabric.ExchangeBind(context, "input-exchange", "output-exchange", null!);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                fabric.ExchangeBind(context, "output-exchange", "Namespace.A", null!));

            Assert.Equal("The exchange binding would create a cycle in the messaging fabric.", exception.Message);
            IMessageExchange<FabricMessage> namespaceA = fabric.GetExchange(context, "Namespace.A", ExchangeType.FanOut);
            IMessageExchange<FabricMessage> inputExchange = fabric.GetExchange(context, "input-exchange", ExchangeType.FanOut);
            IMessageExchange<FabricMessage> outputExchange = fabric.GetExchange(context, "output-exchange", ExchangeType.FanOut);
            Assert.Same(inputExchange, Assert.Single(namespaceA.Sinks));
            Assert.Same(outputExchange, Assert.Single(inputExchange.Sinks));
            Assert.Empty(outputExchange.Sinks);
        }
        finally
        {
            await fabric.Stop(CancellationToken.None);
        }
    }

    private sealed record FabricMessage;
}

public sealed class MessageTopicExchangeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "hash-pattern")]
    public async Task HashPattern_DeliversTheRoutedMessage()
    {
        var exchange = new MessageTopicExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, "#");

        Delivery delivery = await Deliver(exchange, "alpha", "matching");

        Assert.Equal(new[] { new ReceivedMessage("alpha", "matching") }, sink.Messages);
        Assert.True(delivery.WasAlreadyDelivered(sink));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "single-segment-wildcard")]
    public async Task SingleSegmentWildcard_DeliversOnlyTheMatchingRoute()
    {
        var exchange = new MessageTopicExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, "car.*");

        await Deliver(exchange, "bus.red", "bad-red");
        await Deliver(exchange, "bus.green", "bad-green");
        Delivery matchingDelivery = await Deliver(exchange, "car.blue", "good");

        Assert.Equal(new[] { new ReceivedMessage("car.blue", "good") }, sink.Messages);
        Assert.True(matchingDelivery.WasAlreadyDelivered(sink));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "three-segment-wildcard")]
    public async Task ThreeSegmentWildcard_DeliversOnlyTheMatchingRoute()
    {
        var exchange = new MessageTopicExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, "car.*.large");

        await Deliver(exchange, "bus.red.large", "bad-bus-large");
        await Deliver(exchange, "car.green.small", "bad-car-small");
        await Deliver(exchange, "bus.green.small", "bad-bus-small");
        Delivery matchingDelivery = await Deliver(exchange, "car.blue.large", "good");

        Assert.Equal(new[] { new ReceivedMessage("car.blue.large", "good") }, sink.Messages);
        Assert.True(matchingDelivery.WasAlreadyDelivered(sink));
    }

    private static async Task<Delivery> Deliver(MessageTopicExchange<TopicMessage> exchange, string routingKey, string value)
    {
        var delivery = new Delivery(new TopicMessage(value), routingKey, TestContext.Current.CancellationToken);
        await exchange.Deliver(delivery);
        return delivery;
    }

    private sealed class RecordingSink :
        IMessageSink<TopicMessage>
    {
        public List<ReceivedMessage> Messages { get; } = [];

        public Task Deliver(DeliveryContext<TopicMessage> context)
        {
            Messages.Add(new ReceivedMessage(context.RoutingKey!, context.Message.Value));
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class Delivery :
        DeliveryContext<TopicMessage>
    {
        private readonly HashSet<IMessageSink<TopicMessage>> _delivered = [];

        public Delivery(TopicMessage message, string routingKey, CancellationToken cancellationToken)
        {
            Message = message;
            RoutingKey = routingKey;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }
        public TopicMessage Message { get; }
        public string RoutingKey { get; }
        public DateTime? EnqueueTime => null;
        public long? ReceiverId => null;

        public bool WasAlreadyDelivered(IMessageSink<TopicMessage> sink) => _delivered.Contains(sink);

        public void Delivered(IMessageSink<TopicMessage> sink) => _delivered.Add(sink);
    }

    private sealed record TopicMessage(string Value);
    private sealed record ReceivedMessage(string RoutingKey, string Value);
}
