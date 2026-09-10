using System.Collections.Concurrent;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Fabric;

public sealed class MessageExchangeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "direct-exact-routing")]
    public async Task DirectExchange_DeliversOnlyToTheExactRoutingKeyAsync()
    {
        var exchange = new DirectMessageExchange<ExchangeMessage>("direct");
        var empty = new RecordingSink();
        var alpha = new RecordingSink();
        var other = new RecordingSink();
        using ConnectHandle emptyConnection = exchange.Connect(empty, null);
        using ConnectHandle alphaConnection = exchange.Connect(alpha, "alpha");
        using ConnectHandle otherConnection = exchange.Connect(other, "other");

        await exchange.DeliverAsync(new Delivery("empty", null), TestContext.Current.CancellationToken);
        await exchange.DeliverAsync(new Delivery("alpha", "alpha"), TestContext.Current.CancellationToken);
        await exchange.DeliverAsync(new Delivery("missing", "missing"), TestContext.Current.CancellationToken);

        Assert.Equal(["empty"], empty.Values);
        Assert.Equal(["alpha"], alpha.Values);
        Assert.Empty(other.Values);
        Assert.Equal(InMemoryExchangeType.Direct, exchange.ExchangeType);
        Assert.Equal("Exchange(direct)", exchange.ToString());
        Assert.NotNull(exchange.GetProbeResult(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "direct-routing-comparer-and-connection-lifetime")]
    public async Task DirectExchange_UsesItsComparerAndHonorsDisconnectionAsync()
    {
        var exchange = new DirectMessageExchange<ExchangeMessage>("direct", StringComparer.OrdinalIgnoreCase);
        var sink = new RecordingSink();
        ConnectHandle connection = exchange.Connect(sink, "Tenant-A");

        await exchange.DeliverAsync(new Delivery("before", "tenant-a"), TestContext.Current.CancellationToken);
        connection.Disconnect();
        await exchange.DeliverAsync(new Delivery("after", "TENANT-A"), TestContext.Current.CancellationToken);

        Assert.Equal(["before"], sink.Values);
        Assert.Empty(exchange.Sinks);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "fan-out-single-delivery-per-sink")]
    public async Task FanOutExchange_DeliversOnceToEachDistinctSinkAsync()
    {
        var exchange = new FanOutMessageExchange<ExchangeMessage>("fan-out");
        var first = new RecordingSink();
        var second = new RecordingSink();
        using ConnectHandle firstConnection = exchange.Connect(first, "ignored");
        using ConnectHandle duplicateConnection = exchange.Connect(first, null);
        using ConnectHandle secondConnection = exchange.Connect(second, null);

        await exchange.DeliverAsync(new Delivery("message", null), TestContext.Current.CancellationToken);

        Assert.Equal(["message"], first.Values);
        Assert.Equal(["message"], second.Values);
        Assert.Equal(InMemoryExchangeType.FanOut, exchange.ExchangeType);
        Assert.Equal("Exchange(fan-out)", exchange.ToString());
        Assert.NotNull(exchange.GetProbeResult(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "exchange-required-inputs")]
    public async Task Exchanges_RejectEveryMissingRequiredInputAsync()
    {
        Assert.Equal("name", Assert.Throws<ArgumentException>(() =>
            new DirectMessageExchange<ExchangeMessage>(" ")).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() =>
            new FanOutMessageExchange<ExchangeMessage>(" ")).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() =>
            new TopicMessageExchange<ExchangeMessage>(" ")).ParamName);

        var direct = new DirectMessageExchange<ExchangeMessage>("direct");
        var fanOut = new FanOutMessageExchange<ExchangeMessage>("fan-out");
        Assert.Equal("sink", Assert.Throws<ArgumentNullException>(() => direct.Connect(null!, null)).ParamName);
        Assert.Equal("sink", Assert.Throws<ArgumentNullException>(() => fanOut.Connect(null!, null)).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            direct.DeliverAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            fanOut.DeliverAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => direct.Probe(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => fanOut.Probe(null!)).ParamName);
    }

    private sealed class RecordingSink : IMessageSink<ExchangeMessage>
    {
        private readonly ConcurrentQueue<string> _values = new();

        public string[] Values => _values.ToArray();

        public Task DeliverAsync(IMessageDeliveryContext<ExchangeMessage> context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _values.Enqueue(context.Message.Value);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class Delivery(string value, string? routingKey) : IMessageDeliveryContext<ExchangeMessage>
    {
        private readonly HashSet<IMessageSink<ExchangeMessage>> _destinations = new(ReferenceEqualityComparer.Instance);

        public CancellationToken CancellationToken => TestContext.Current.CancellationToken;
        public ExchangeMessage Message { get; } = new(value);
        public string? RoutingKey { get; } = routingKey;
        public DateTimeOffset? EnqueueTime => null;

        public bool TryReserveDelivery(IMessageSink<ExchangeMessage> sink)
        {
            ArgumentNullException.ThrowIfNull(sink);
            lock (_destinations)
                return _destinations.Add(sink);
        }
    }

    private sealed record ExchangeMessage(string Value);
}
