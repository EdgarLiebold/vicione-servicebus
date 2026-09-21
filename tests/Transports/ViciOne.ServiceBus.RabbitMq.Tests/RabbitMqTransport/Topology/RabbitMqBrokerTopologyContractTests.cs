using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Topology;

public sealed class RabbitMqBrokerTopologyContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "immutable-complete-probe-snapshot")]
    public void Topology_SnapshotsDeclarationsAndProbesEveryBrokerRelevantValue()
    {
        TopologyFixture fixture = CreateTopologyFixture();
        RabbitMqBrokerTopology topology = fixture.Topology;
        fixture.ClearSourceCollections();

        Assert.Equal(2, topology.Exchanges.Length);
        Assert.Single(topology.Queues);
        Assert.Single(topology.ExchangeBindings);
        Assert.Single(topology.QueueBindings);

        var probe = new RecordingProbeContext();
        ((IProbeSite)topology).Probe(probe);

        Assert.Collection(
            probe.Children,
            first => AssertExchange(first, "source", "direct", durable: true, autoDelete: false),
            second => AssertExchange(second, "destination", "fanout", durable: false, autoDelete: true),
            AssertQueue,
            AssertExchangeBinding,
            AssertQueueBinding);

        Assert.Collection(
            probe.Children[0].Children,
            argument => AssertValues(argument, "argument", ("key", "alternate"), ("value", "fallback")),
            argument => AssertValues(argument, "argument", ("key", "key-only")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "deeply-immutable-snapshot-and-hash-inputs")]
    public void Topology_DefensivelyCopiesArraysAndEveryArgumentDictionary()
    {
        Dictionary<string, object?> exchangeArguments = Arguments();
        Dictionary<string, object?> queueArguments = Arguments();
        Dictionary<string, object?> exchangeBindingArguments = Arguments();
        Dictionary<string, object?> queueBindingArguments = Arguments();
        var exchange = new ExchangeEntity(1, "source", "direct", true, false, exchangeArguments);
        var destination = Exchange(2, "destination");
        var queue = new QueueEntity(3, "orders", true, false, false, queueArguments);
        var exchangeBinding = new ExchangeBindingEntity(4, exchange, destination, "route", exchangeBindingArguments);
        var queueBinding = new QueueBindingEntity(5, exchange, queue, "route", queueBindingArguments);
        var topology = new RabbitMqBrokerTopology([exchange, destination], [exchangeBinding], [queue], [queueBinding]);
        int exchangeHash = ExchangeEntity.EntityComparer.GetHashCode(exchange);
        int queueHash = QueueEntity.QueueComparer.GetHashCode(queue);

        exchangeArguments["a"] = 99;
        queueArguments["a"] = 99;
        exchangeBindingArguments["a"] = 99;
        queueBindingArguments["a"] = 99;
        topology.Exchanges[0] = destination;
        topology.Queues[0] = Queue(9, "changed");
        topology.ExchangeBindings[0] = new ExchangeBindingEntity(9, destination, exchange, "changed", Arguments());
        topology.QueueBindings[0] = new QueueBindingEntity(9, destination, queue, "changed", Arguments());

        Assert.Equal(1, exchange.ExchangeArguments["a"]);
        Assert.Equal(1, queue.QueueArguments["a"]);
        Assert.Equal(1, exchangeBinding.Arguments["a"]);
        Assert.Equal(1, queueBinding.Arguments["a"]);
        Assert.Equal(exchangeHash, ExchangeEntity.EntityComparer.GetHashCode(exchange));
        Assert.Equal(queueHash, QueueEntity.QueueComparer.GetHashCode(queue));
        Assert.Same(exchange, topology.Exchanges[0]);
        Assert.Same(queue, topology.Queues[0]);
        Assert.Same(exchangeBinding, topology.ExchangeBindings[0]);
        Assert.Same(queueBinding, topology.QueueBindings[0]);
        Assert.Throws<NotSupportedException>(() => exchange.ExchangeArguments["a"] = 2);
        Assert.Throws<NotSupportedException>(() => queue.QueueArguments["a"] = 2);
        Assert.Throws<NotSupportedException>(() => exchangeBinding.Arguments["a"] = 2);
        Assert.Throws<NotSupportedException>(() => queueBinding.Arguments["a"] = 2);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "exchange-comparer-contract")]
    public void ExchangeComparers_UseEveryDeclarationFieldAndStableArgumentOrdering()
    {
        var exchange = new ExchangeEntity(1, "orders", "direct", true, false, Arguments(reverse: true));
        var equivalentExchange = new ExchangeEntity(99, "orders", "direct", true, false, Arguments());

        AssertComparerContract(ExchangeEntity.NameComparer, exchange, equivalentExchange);
        AssertComparerContract(ExchangeEntity.EntityComparer, exchange, equivalentExchange);
        var sameNameDifferentDeclaration = new ExchangeEntity(2, "orders", "topic", false, true,
            new Dictionary<string, object?> { ["changed"] = true });
        Assert.True(ExchangeEntity.NameComparer.Equals(exchange, sameNameDifferentDeclaration));
        Assert.Equal(ExchangeEntity.NameComparer.GetHashCode(exchange), ExchangeEntity.NameComparer.GetHashCode(sameNameDifferentDeclaration));
        Assert.False(ExchangeEntity.NameComparer.Equals(exchange, new ExchangeEntity(2, "other", "direct", true, false, Arguments())));
        Assert.All(
            new[]
            {
                new ExchangeEntity(2, "other", "direct", true, false, exchange.ExchangeArguments),
                new ExchangeEntity(2, "orders", "topic", true, false, exchange.ExchangeArguments),
                new ExchangeEntity(2, "orders", "direct", false, false, exchange.ExchangeArguments),
                new ExchangeEntity(2, "orders", "direct", true, true, exchange.ExchangeArguments),
                new ExchangeEntity(2, "orders", "direct", true, false, new Dictionary<string, object?> { ["a"] = 1 }),
                new ExchangeEntity(2, "orders", "direct", true, false, new Dictionary<string, object?> { ["a"] = 1, ["b"] = 3 }),
            },
            changed => Assert.False(ExchangeEntity.EntityComparer.Equals(exchange, changed)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "queue-comparer-contract")]
    public void QueueComparers_UseEveryDeclarationFieldAndStableArgumentOrdering()
    {
        var queue = new QueueEntity(3, "orders", true, false, true, Arguments(reverse: true));
        var equivalentQueue = new QueueEntity(88, "orders", true, false, true, Arguments());

        AssertComparerContract(QueueEntity.NameComparer, queue, equivalentQueue);
        AssertComparerContract(QueueEntity.QueueComparer, queue, equivalentQueue);
        var sameNameDifferentDeclaration = new QueueEntity(4, "orders", false, true, false,
            new Dictionary<string, object?> { ["changed"] = true });
        Assert.True(QueueEntity.NameComparer.Equals(queue, sameNameDifferentDeclaration));
        Assert.Equal(QueueEntity.NameComparer.GetHashCode(queue), QueueEntity.NameComparer.GetHashCode(sameNameDifferentDeclaration));
        Assert.False(QueueEntity.NameComparer.Equals(queue, new QueueEntity(4, "other", true, false, true, Arguments())));
        Assert.All(
            new[]
            {
                new QueueEntity(4, "other", true, false, true, queue.QueueArguments),
                new QueueEntity(4, "orders", false, false, true, queue.QueueArguments),
                new QueueEntity(4, "orders", true, true, true, queue.QueueArguments),
                new QueueEntity(4, "orders", true, false, false, queue.QueueArguments),
                new QueueEntity(4, "orders", true, false, true, new Dictionary<string, object?> { ["a"] = 1 }),
                new QueueEntity(4, "orders", true, false, true, new Dictionary<string, object?> { ["a"] = 1, ["b"] = 3 }),
            },
            changed => Assert.False(QueueEntity.QueueComparer.Equals(queue, changed)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "exchange-binding-comparer-contract")]
    public void ExchangeBindingComparer_UsesNodeIdentityRoutingKeyAndStableArguments()
    {
        var source = Exchange(1, "source");
        var destination = Exchange(2, "destination");
        var binding = new ExchangeBindingEntity(3, source, destination, "route", Arguments(reverse: true));
        var equivalent = new ExchangeBindingEntity(99, source, destination, "route", Arguments());

        AssertComparerContract(ExchangeBindingEntity.EntityComparer, binding, equivalent);
        Assert.False(ExchangeBindingEntity.EntityComparer.Equals(
            binding,
            new ExchangeBindingEntity(4, source, destination, "other", binding.Arguments)));
        Assert.False(ExchangeBindingEntity.EntityComparer.Equals(
            binding,
            new ExchangeBindingEntity(4, Exchange(5, "source"), destination, "route", binding.Arguments)));
        Assert.False(ExchangeBindingEntity.EntityComparer.Equals(
            binding,
            new ExchangeBindingEntity(4, source, Exchange(5, "destination"), "route", binding.Arguments)));
        Assert.False(ExchangeBindingEntity.EntityComparer.Equals(
            binding,
            new ExchangeBindingEntity(4, source, destination, "route", new Dictionary<string, object?> { ["a"] = 1 })));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "queue-binding-comparer-contract")]
    public void QueueBindingComparer_UsesNodeIdentityRoutingKeyAndStableArguments()
    {
        var exchange = Exchange(1, "source");
        var queue = Queue(2, "orders");
        var binding = new QueueBindingEntity(3, exchange, queue, "route", Arguments(reverse: true));
        var equivalent = new QueueBindingEntity(99, exchange, queue, "route", Arguments());

        AssertComparerContract(QueueBindingEntity.EntityComparer, binding, equivalent);
        Assert.False(QueueBindingEntity.EntityComparer.Equals(
            binding,
            new QueueBindingEntity(4, exchange, queue, "other", binding.Arguments)));
        Assert.False(QueueBindingEntity.EntityComparer.Equals(
            binding,
            new QueueBindingEntity(4, Exchange(5, "source"), queue, "route", binding.Arguments)));
        Assert.False(QueueBindingEntity.EntityComparer.Equals(
            binding,
            new QueueBindingEntity(4, exchange, Queue(5, "orders"), "route", binding.Arguments)));
        Assert.False(QueueBindingEntity.EntityComparer.Equals(
            binding,
            new QueueBindingEntity(4, exchange, queue, "route", new Dictionary<string, object?> { ["a"] = 1 })));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-BROKER-TOPOLOGY", "complete-diagnostic-descriptions")]
    public void DiagnosticDescriptions_ExposeEveryConfiguredBrokerValue()
    {
        (ExchangeEntity exchange, QueueEntity queue, ExchangeBindingEntity exchangeBinding, QueueBindingEntity queueBinding) =
            CreateDiagnosticEntities();

        Assert.Equal("name: orders, type: direct, durable, auto-delete, alternate: fallback", exchange.ToString());
        Assert.Equal("name: orders, durable, auto-delete, exclusive, x-max-length: 100", queue.ToString());
        Assert.Equal(
            "source: orders, destination: destination, routing-key: exchange.route, priority: 7",
            exchangeBinding.ToString());
        Assert.Equal("source: orders, destination: orders, mode: all", queueBinding.ToString());
    }

    private static TopologyFixture CreateTopologyFixture()
    {
        var source = new ExchangeEntity(1, "source", "direct", true, false, new Dictionary<string, object?>
        {
            ["alternate"] = "fallback",
            ["key-only"] = null,
        });
        var destination = new ExchangeEntity(2, "destination", "fanout", false, true, new Dictionary<string, object?>());
        var queue = new QueueEntity(3, "orders", true, false, true, new Dictionary<string, object?>
        {
            ["x-max-length"] = 100,
        });
        var exchangeBinding = new ExchangeBindingEntity(4, source, destination, "exchange.route", new Dictionary<string, object?>
        {
            ["exchange-argument"] = 7,
        });
        var queueBinding = new QueueBindingEntity(5, destination, queue, "queue.route", new Dictionary<string, object?>
        {
            ["queue-argument"] = "value",
        });
        var exchanges = new List<ExchangeEntity> { source, destination };
        var queues = new List<QueueEntity> { queue };
        var exchangeBindings = new List<ExchangeBindingEntity> { exchangeBinding };
        var queueBindings = new List<QueueBindingEntity> { queueBinding };
        var topology = new RabbitMqBrokerTopology(exchanges, exchangeBindings, queues, queueBindings);

        return new TopologyFixture(topology, exchanges, queues, exchangeBindings, queueBindings);
    }

    private static (ExchangeEntity Exchange, QueueEntity Queue, ExchangeBindingEntity ExchangeBinding, QueueBindingEntity QueueBinding)
        CreateDiagnosticEntities()
    {
        var exchange = new ExchangeEntity(1, "orders", "direct", true, true, new Dictionary<string, object?>
        {
            ["alternate"] = "fallback",
        });
        var queue = new QueueEntity(2, "orders", true, true, true, new Dictionary<string, object?>
        {
            ["x-max-length"] = 100,
        });
        var destination = Exchange(3, "destination");
        var exchangeBinding = new ExchangeBindingEntity(4, exchange, destination, "exchange.route", new Dictionary<string, object?>
        {
            ["priority"] = 7,
        });
        var queueBinding = new QueueBindingEntity(5, exchange, queue, "", new Dictionary<string, object?>
        {
            ["mode"] = "all",
        });

        return (exchange, queue, exchangeBinding, queueBinding);
    }

    private static Dictionary<string, object?> Arguments(bool reverse = false) =>
        reverse
            ? new Dictionary<string, object?> { ["b"] = 2, ["a"] = 1 }
            : new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 };

    private static ExchangeEntity Exchange(long id, string name) =>
        new(id, name, "fanout", true, false, new Dictionary<string, object?>());

    private static QueueEntity Queue(long id, string name) =>
        new(id, name, true, false, false, new Dictionary<string, object?>());

    private static void AssertComparerContract<T>(IEqualityComparer<T> comparer, T value, T equivalent)
        where T : class
    {
        Assert.True(comparer.Equals(value, value));
        Assert.True(comparer.Equals(null, null));
        Assert.False(comparer.Equals(value, null));
        Assert.False(comparer.Equals(null, value));
        Assert.True(comparer.Equals(value, equivalent));
        Assert.Equal(comparer.GetHashCode(value), comparer.GetHashCode(equivalent));
    }

    private static void AssertExchange(RecordingProbeContext scope, string name, string type, bool durable, bool autoDelete)
    {
        AssertValues(scope, "exchange", ("Name", name), ("Type", type), ("Durable", durable), ("AutoDelete", autoDelete));
    }

    private static void AssertQueue(RecordingProbeContext scope)
    {
        AssertValues(scope, "queue", ("Name", "orders"), ("Durable", true), ("AutoDelete", false), ("Exclusive", true));
        Assert.Collection(
            scope.Children,
            argument => AssertValues(argument, "argument", ("key", "x-max-length"), ("value", 100)));
    }

    private static void AssertExchangeBinding(RecordingProbeContext scope)
    {
        AssertValues(
            scope,
            "exchange-binding",
            ("Source", "source"),
            ("Destination", "destination"),
            ("RoutingKey", "exchange.route"));
        Assert.Collection(
            scope.Children,
            argument => AssertValues(argument, "argument", ("key", "exchange-argument"), ("value", 7)));
    }

    private static void AssertQueueBinding(RecordingProbeContext scope)
    {
        AssertValues(
            scope,
            "queue-binding",
            ("Source", "destination"),
            ("Destination", "orders"),
            ("RoutingKey", "queue.route"));
        Assert.Collection(
            scope.Children,
            argument => AssertValues(argument, "argument", ("key", "queue-argument"), ("value", "value")));
    }

    private static void AssertValues(RecordingProbeContext scope, string key, params (string Key, object? Value)[] expected)
    {
        Assert.Equal(key, scope.Key);
        Assert.Equal(expected.Length, scope.Values.Count);
        foreach ((string valueKey, object? value) in expected)
            Assert.Equal(value, scope.Values[valueKey]);
    }

    private sealed class RecordingProbeContext(string? key = null) : ProbeContext
    {
        public CancellationToken CancellationToken => default;
        public string? Key { get; } = key;
        public Dictionary<string, object?> Values { get; } = [];
        public List<RecordingProbeContext> Children { get; } = [];

        public void Add(string key, string? value) => Values[key] = value;

        public void Add(string key, object? value) => Values[key] = value;

        public void Set(object values)
        {
            foreach (var property in values.GetType().GetProperties())
                Values[property.Name] = property.GetValue(values);
        }

        public void Set(IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach ((string valueKey, object? value) in values)
                Values[valueKey] = value;
        }

        public ProbeContext CreateScope(string childKey)
        {
            var child = new RecordingProbeContext(childKey);
            Children.Add(child);
            return child;
        }
    }

    private sealed record TopologyFixture(
        RabbitMqBrokerTopology Topology,
        List<ExchangeEntity> Exchanges,
        List<QueueEntity> Queues,
        List<ExchangeBindingEntity> ExchangeBindings,
        List<QueueBindingEntity> QueueBindings)
    {
        public void ClearSourceCollections()
        {
            Exchanges.Clear();
            Queues.Clear();
            ExchangeBindings.Clear();
            QueueBindings.Clear();
        }
    }
}
