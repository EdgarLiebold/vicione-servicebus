using System.Collections.Concurrent;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Fabric;

public sealed class TopicMessageExchangeTests
{
    [Theory]
    [InlineData("#", "", true)]
    [InlineData("#", "alpha.beta", true)]
    [InlineData("#.completed", "completed", true)]
    [InlineData("#.completed", "orders.eu.completed", true)]
    [InlineData("orders.#.completed", "orders.completed", true)]
    [InlineData("orders.#.completed", "orders.eu.priority.completed", true)]
    [InlineData("orders.#.completed", "orders.eu.priority.failed", false)]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "hash-wildcard-at-any-segment")]
    public async Task HashWildcard_MatchesZeroOrMoreSegmentsAtAnyPositionAsync(
        string pattern,
        string routingKey,
        bool expected)
    {
        var exchange = new TopicMessageExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, pattern);

        Delivery delivery = await DeliverAsync(exchange, routingKey, "message");

        Assert.Equal(expected ? 1 : 0, sink.Messages.Length);
        Assert.Equal(expected, delivery.IsReserved(sink));
    }

    [Theory]
    [InlineData("orders.*", "orders.created", true)]
    [InlineData("orders.*", "orders", false)]
    [InlineData("orders.*", "orders.eu.created", false)]
    [InlineData("*.created", "orders.created", true)]
    [InlineData("*.created", "orders.failed", false)]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "single-segment-wildcard")]
    public async Task StarWildcard_MatchesExactlyOneSegmentAsync(string pattern, string routingKey, bool expected)
    {
        var exchange = new TopicMessageExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, pattern);

        await DeliverAsync(exchange, routingKey, "message");

        Assert.Equal(expected ? 1 : 0, sink.Messages.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "empty-pattern-matches-empty-route")]
    public async Task EmptyPattern_MatchesOnlyAnEmptyRoutingKeyAsync()
    {
        var exchange = new TopicMessageExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, null);

        await DeliverAsync(exchange, "orders.created", "not-matching");
        await DeliverAsync(exchange, string.Empty, "matching");

        Assert.Equal([new ConsumedMessage(string.Empty, "matching")], sink.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "overlapping-patterns-deliver-once")]
    public async Task OverlappingPatterns_DeliverToTheSameSinkOnlyOnceAsync()
    {
        var exchange = new TopicMessageExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle all = exchange.Connect(sink, "#");
        using ConnectHandle orders = exchange.Connect(sink, "orders.*");
        using ConnectHandle exact = exchange.Connect(sink, "orders.created");

        Delivery delivery = await DeliverAsync(exchange, "orders.created", "created");

        Assert.Equal([new ConsumedMessage("orders.created", "created")], sink.Messages);
        Assert.True(delivery.IsReserved(sink));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "custom-segment-comparer")]
    public async Task ConfiguredComparer_AppliesToPatternsAndLiteralSegmentsAsync()
    {
        var exchange = new TopicMessageExchange<TopicMessage>("test-exchange", StringComparer.OrdinalIgnoreCase);
        var sink = new RecordingSink();
        using ConnectHandle first = exchange.Connect(sink, "Orders.Created");
        using ConnectHandle second = exchange.Connect(sink, "orders.created");

        await DeliverAsync(exchange, "ORDERS.CREATED", "created");

        Assert.Single(exchange.Sinks);
        Assert.Single(sink.Messages);
        Assert.Equal(InMemoryExchangeType.Topic, exchange.ExchangeType);
        Assert.Equal("Exchange(test-exchange)", exchange.ToString());
        Assert.NotNull(exchange.GetProbeResult(TestContext.Current.CancellationToken));
        Assert.Equal("sink", Assert.Throws<ArgumentNullException>(() => exchange.Connect(null!, "#")).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            exchange.DeliverAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => exchange.Probe(null!)).ParamName);
    }

    [Theory]
    [InlineData("orders..created")]
    [InlineData("orders.creat*ed")]
    [InlineData("orders.#suffix")]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "invalid-pattern-rejected")]
    public void InvalidPattern_IsRejectedBeforeAConnectionIsCreated(string pattern)
    {
        var exchange = new TopicMessageExchange<TopicMessage>("test-exchange");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => exchange.Connect(new RecordingSink(), pattern));

        Assert.Equal("pattern", exception.ParamName);
        Assert.Empty(exchange.Sinks);
    }

    [Theory]
    [InlineData("orders..created")]
    [InlineData("orders.*")]
    [InlineData("orders.#")]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "invalid-routing-key-rejected")]
    public async Task InvalidRoutingKey_IsRejectedBeforeDeliveryAsync(string routingKey)
    {
        var exchange = new TopicMessageExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, "#");

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            DeliverAsync(exchange, routingKey, "message"));

        Assert.Equal("routingKey", exception.ParamName);
        Assert.Empty(sink.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "connection-lifetime")]
    public async Task DisconnectedPattern_StopsReceivingMessagesAsync()
    {
        var exchange = new TopicMessageExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        ConnectHandle connection = exchange.Connect(sink, "orders.*");

        await DeliverAsync(exchange, "orders.created", "before");
        connection.Disconnect();
        await DeliverAsync(exchange, "orders.updated", "after");

        Assert.Equal([new ConsumedMessage("orders.created", "before")], sink.Messages);
        Assert.Empty(exchange.Sinks);
    }

    private static async Task<Delivery> DeliverAsync(
        TopicMessageExchange<TopicMessage> exchange,
        string routingKey,
        string value)
    {
        var delivery = new Delivery(new TopicMessage(value), routingKey, TestContext.Current.CancellationToken);
        await exchange.DeliverAsync(delivery, TestContext.Current.CancellationToken);
        return delivery;
    }

    private sealed class RecordingSink :
        IMessageSink<TopicMessage>
    {
        private readonly ConcurrentQueue<ConsumedMessage> _messages = new();

        public ConsumedMessage[] Messages => _messages.ToArray();

        public Task DeliverAsync(IMessageDeliveryContext<TopicMessage> context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _messages.Enqueue(new ConsumedMessage(context.RoutingKey!, context.Message.Value));
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class Delivery :
        IMessageDeliveryContext<TopicMessage>
    {
        private readonly HashSet<IMessageSink<TopicMessage>> _delivered = new(ReferenceEqualityComparer.Instance);

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

        public bool IsReserved(IMessageSink<TopicMessage> sink)
        {
            lock (_delivered)
                return _delivered.Contains(sink);
        }

        public bool TryReserveDelivery(IMessageSink<TopicMessage> sink)
        {
            ArgumentNullException.ThrowIfNull(sink);
            lock (_delivered)
                return _delivered.Add(sink);
        }
    }

    private sealed record TopicMessage(string Value);

    private sealed record ConsumedMessage(string RoutingKey, string Value);
}
