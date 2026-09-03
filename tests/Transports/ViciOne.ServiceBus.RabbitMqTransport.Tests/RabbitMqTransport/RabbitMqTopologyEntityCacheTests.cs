using ViciOne.ServiceBus.RabbitMqTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqTransport;

public sealed class RabbitMqTopologyEntityCacheTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TOPOLOGY-CACHE", "stable-declaration-single-flight")]
    public async Task StableDeclarations_AreSingleFlightAndConnectionOwned()
    {
        var cache = new RabbitMqTopologyEntityCache();
        var entered = NewSignal();
        var release = NewSignal();
        var invocations = 0;
        var receivedCancelableToken = true;
        var exchange = Exchange("orders");

        Task Declare(CancellationToken token)
        {
            receivedCancelableToken = token.CanBeCanceled;
            Interlocked.Increment(ref invocations);
            entered.TrySetResult();
            return release.Task;
        }

        Task first = cache.DeclareExchange(exchange, Declare, TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task[] followers = Enumerable.Range(0, 31)
            .Select(_ => cache.DeclareExchange(exchange, Declare, TestContext.Current.CancellationToken))
            .ToArray();

        try
        {
            Assert.Equal(1, Volatile.Read(ref invocations));
        }
        finally
        {
            release.TrySetResult();
        }
        await Task.WhenAll(followers.Prepend(first));

        Assert.Equal(1, invocations);
        Assert.False(receivedCancelableToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TOPOLOGY-CACHE", "caller-cancellation-does-not-evict-shared-work")]
    public async Task CanceledWaiter_DoesNotEvictTheSharedDeclarationOrStartADuplicate()
    {
        var cache = new RabbitMqTopologyEntityCache();
        var entered = NewSignal();
        var release = NewSignal();
        var invocations = 0;
        var exchange = Exchange("orders");

        Task Declare(CancellationToken _)
        {
            Interlocked.Increment(ref invocations);
            entered.TrySetResult();
            return release.Task;
        }

        Task owner = cache.DeclareExchange(exchange, Declare, TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        using var canceledWait = new CancellationTokenSource();
        Task waiter = cache.DeclareExchange(exchange, Declare, canceledWait.Token);
        canceledWait.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
        Assert.Equal(canceledWait.Token, exception.CancellationToken);

        Task laterWaiter = cache.DeclareExchange(exchange, Declare, TestContext.Current.CancellationToken);
        try
        {
            Assert.Equal(1, Volatile.Read(ref invocations));
        }
        finally
        {
            release.TrySetResult();
        }
        await Task.WhenAll(owner, laterWaiter);
        Assert.Equal(1, invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TOPOLOGY-CACHE", "fault-eviction-and-retry")]
    public async Task FaultedDeclaration_IsEvictedAndTheNextCallerRetries()
    {
        var cache = new RabbitMqTopologyEntityCache();
        var expected = new InvalidOperationException("declare failed");
        var invocations = 0;
        var queue = Queue("orders");

        Task Declare(CancellationToken _) => Interlocked.Increment(ref invocations) == 1
            ? Task.FromException(expected)
            : Task.CompletedTask;

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cache.DeclareQueue(queue, Declare, TestContext.Current.CancellationToken));
        await cache.DeclareQueue(queue, Declare, TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.Equal(2, invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TOPOLOGY-CACHE", "generation-invalidation-redeclares")]
    public async Task InvalidationRacingADeclaration_ForcesTheWaitingCallerToRedeclare()
    {
        var cache = new RabbitMqTopologyEntityCache();
        var entered = NewSignal();
        var release = NewSignal();
        var invocations = 0;

        Task Declare(CancellationToken _)
        {
            Interlocked.Increment(ref invocations);
            entered.TrySetResult();
            return release.Task;
        }

        Task declaration = cache.DeclareExchange(
            Exchange("orders"),
            Declare,
            TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        cache.Invalidate();
        release.TrySetResult();
        await declaration;

        Assert.Equal(2, invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TOPOLOGY-CACHE", "immutable-definition-snapshot")]
    public async Task DefinitionSnapshot_IsImmutableAndConflictsFailBeforeProviderWork()
    {
        var cache = new RabbitMqTopologyEntityCache();
        var invocations = 0;
        var mutableArguments = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["mode"] = "original",
        };
        var original = Queue("orders", mutableArguments);

        await cache.DeclareQueue(original, _ =>
        {
            invocations++;
            return Task.CompletedTask;
        }, TestContext.Current.CancellationToken);
        mutableArguments["mode"] = "mutated";

        await cache.DeclareQueue(
            Queue("orders", new Dictionary<string, object> { ["mode"] = "original" }),
            _ =>
            {
                invocations++;
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);
        ConfigurationException conflict = await Assert.ThrowsAsync<ConfigurationException>(() =>
            cache.DeclareQueue(original, _ =>
            {
                invocations++;
                return Task.CompletedTask;
            }, TestContext.Current.CancellationToken));

        Assert.Equal(1, invocations);
        Assert.Contains("conflicting definitions", conflict.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TOPOLOGY-CACHE", "canonical-nested-definition")]
    public async Task CanonicalDefinition_IgnoresMapOrderButRetainsNestedTypesAndValues()
    {
        var cache = new RabbitMqTopologyEntityCache();
        var invocations = 0;
        var first = Queue("orders", new Dictionary<string, object>
        {
            ["z"] = new object[] { "value", 7 },
            ["a"] = new Dictionary<string, object> { ["right"] = 2L, ["left"] = true },
        });
        var equivalent = Queue("orders", new Dictionary<string, object>
        {
            ["a"] = new Dictionary<string, object> { ["left"] = true, ["right"] = 2L },
            ["z"] = new List<object> { "value", 7 },
        });
        var changedType = Queue("orders", new Dictionary<string, object>
        {
            ["a"] = new Dictionary<string, object> { ["left"] = true, ["right"] = 2 },
            ["z"] = new object[] { "value", 7 },
        });

        Task Declare(CancellationToken _)
        {
            invocations++;
            return Task.CompletedTask;
        }

        await cache.DeclareQueue(first, Declare, TestContext.Current.CancellationToken);
        await cache.DeclareQueue(equivalent, Declare, TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<ConfigurationException>(
            () => cache.DeclareQueue(changedType, Declare, TestContext.Current.CancellationToken));

        Assert.Equal(1, invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TOPOLOGY-CACHE", "structured-entity-key")]
    public async Task StructuredBindingKeys_DoNotAliasNamesContainingDelimiters()
    {
        var cache = new RabbitMqTopologyEntityCache();
        var invocations = 0;
        var first = new TestExchangeToQueueBinding(Exchange("a:b"), Queue("c"), "d", Arguments());
        var second = new TestExchangeToQueueBinding(Exchange("a"), Queue("b:c"), "d", Arguments());

        Task Bind(CancellationToken _)
        {
            invocations++;
            return Task.CompletedTask;
        }

        await cache.Bind(first, Bind, TestContext.Current.CancellationToken);
        await cache.Bind(second, Bind, TestContext.Current.CancellationToken);

        Assert.Equal(2, invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TOPOLOGY-CACHE", "transient-topology-remains-channel-scoped")]
    public async Task TransientEntitiesAndBindings_AreNeverCached()
    {
        var cache = new RabbitMqTopologyEntityCache();
        var invocations = 0;
        var transientExchange = Exchange("temporary", durable: false, autoDelete: true);
        var transientQueue = Queue("temporary", durable: false, exclusive: true, autoDelete: true);
        var binding = new TestExchangeToQueueBinding(transientExchange, transientQueue, string.Empty, Arguments());

        Task Invoke(CancellationToken token)
        {
            Assert.Equal(TestContext.Current.CancellationToken, token);
            invocations++;
            return Task.CompletedTask;
        }

        await cache.DeclareExchange(transientExchange, Invoke, TestContext.Current.CancellationToken);
        await cache.DeclareExchange(transientExchange, Invoke, TestContext.Current.CancellationToken);
        await cache.DeclareQueue(transientQueue, Invoke, TestContext.Current.CancellationToken);
        await cache.DeclareQueue(transientQueue, Invoke, TestContext.Current.CancellationToken);
        await cache.Bind(binding, Invoke, TestContext.Current.CancellationToken);
        await cache.Bind(binding, Invoke, TestContext.Current.CancellationToken);

        Assert.Equal(6, invocations);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static IDictionary<string, object> Arguments() =>
        new Dictionary<string, object>(StringComparer.Ordinal);

    private static TestExchange Exchange(
        string name,
        bool durable = true,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null) =>
        new(name, RabbitMQ.Client.ExchangeType.Direct, durable, autoDelete, arguments ?? Arguments());

    private static TestQueue Queue(
        string name,
        IDictionary<string, object>? arguments = null,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false) =>
        new(name, durable, exclusive, autoDelete, arguments ?? Arguments());

    private sealed record TestExchange(
        string ExchangeName,
        string ExchangeType,
        bool Durable,
        bool AutoDelete,
        IDictionary<string, object> ExchangeArguments) : Exchange;

    private sealed record TestQueue(
        string QueueName,
        bool Durable,
        bool Exclusive,
        bool AutoDelete,
        IDictionary<string, object> QueueArguments) : Queue;

    private sealed record TestExchangeToQueueBinding(
        Exchange Source,
        Queue Destination,
        string RoutingKey,
        IDictionary<string, object> Arguments) : ExchangeToQueueBinding;
}
