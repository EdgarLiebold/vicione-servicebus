using System.Collections.Concurrent;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Fabric;

public sealed class MessageTopicExchangeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "hash-pattern")]
    public async Task HashPattern_DeliversTheRoutedMessageAsync()
    {
        var exchange = new MessageTopicExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, "#");

        Delivery delivery = await DeliverAsync(exchange, "alpha", "matching");

        Assert.Equal(new[] { new ReceivedMessage("alpha", "matching") }, sink.Messages);
        Assert.True(delivery.WasAlreadyDelivered(sink));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "single-segment-wildcard")]
    public async Task SingleSegmentWildcard_DeliversOnlyTheMatchingRouteAsync()
    {
        var exchange = new MessageTopicExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, "car.*");

        await DeliverAsync(exchange, "bus.red", "bad-red");
        await DeliverAsync(exchange, "bus.green", "bad-green");
        Delivery matchingDelivery = await DeliverAsync(exchange, "car.blue", "good");

        Assert.Equal(new[] { new ReceivedMessage("car.blue", "good") }, sink.Messages);
        Assert.True(matchingDelivery.WasAlreadyDelivered(sink));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "three-segment-wildcard")]
    public async Task ThreeSegmentWildcard_DeliversOnlyTheMatchingRouteAsync()
    {
        var exchange = new MessageTopicExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, "car.*.large");

        await DeliverAsync(exchange, "bus.red.large", "bad-bus-large");
        await DeliverAsync(exchange, "car.green.small", "bad-car-small");
        await DeliverAsync(exchange, "bus.green.small", "bad-bus-small");
        Delivery matchingDelivery = await DeliverAsync(exchange, "car.blue.large", "good");

        Assert.Equal(new[] { new ReceivedMessage("car.blue.large", "good") }, sink.Messages);
        Assert.True(matchingDelivery.WasAlreadyDelivered(sink));
    }

    private static async Task<Delivery> DeliverAsync(MessageTopicExchange<TopicMessage> exchange, string routingKey, string value)
    {
        var delivery = new Delivery(new TopicMessage(value), routingKey, TestContext.Current.CancellationToken);
        await exchange.DeliverAsync(delivery);
        return delivery;
    }

    private sealed class RecordingSink :
        IMessageSink<TopicMessage>
    {
        public List<ReceivedMessage> Messages { get; } = [];

        public Task DeliverAsync(DeliveryContext<TopicMessage> context, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); Messages.Add(new ReceivedMessage(context.RoutingKey!, context.Message.Value));
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
        public DateTimeOffset? EnqueueTime => null;
        public long? ReceiverId => null;

        public bool WasAlreadyDelivered(IMessageSink<TopicMessage> sink) => _delivered.Contains(sink);

        public void Delivered(IMessageSink<TopicMessage> sink) => _delivered.Add(sink);
    }

    private sealed record TopicMessage(string Value);
    private sealed record ReceivedMessage(string RoutingKey, string Value);
}
