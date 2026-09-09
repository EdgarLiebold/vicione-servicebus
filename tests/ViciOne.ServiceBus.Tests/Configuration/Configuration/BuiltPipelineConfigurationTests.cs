using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class BuiltPipelineConfigurationTests
{
    private const int BusConcurrencyLimit = 3;
    private const int BusRateLimit = 1_000;
    private const int ComponentConcurrencyLimit = 1;
    private const int EndpointConcurrencyLimit = 7;
    private const int EndpointRateLimit = 100;

    [Fact]
    [RequirementCoverage("REQ-VSB-BUILT-PIPELINE", "complete-configured-filter-and-component-shape")]
    public async Task StartedBusProbe_ContainsEveryConfiguredFilterComponentPersistenceAndLimitAsync()
    {
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
        {
            configuration.UseTransform<PipelineMessage>(_ => { });
            configuration.UseConcurrencyLimit(BusConcurrencyLimit);
            configuration.UseRateLimit(BusRateLimit);
            configuration.UseTransaction();
            configuration.UseMessageRetry(retry => retry.Immediate(3));

            configuration.ReceiveEndpoint($"pipeline-{NewId.NextGuid():N}", endpoint =>
            {
                endpoint.Saga(new InMemorySagaRepository<PipelineSaga>(), saga =>
                {
                    saga.UseConcurrencyLimit(ComponentConcurrencyLimit);
                    saga.UseRateLimit(BusRateLimit);
                });
                endpoint.Consumer<PipelineConsumer>(consumer =>
                {
                    consumer.UseConcurrencyLimit(ComponentConcurrencyLimit);
                    consumer.UseRateLimit(EndpointRateLimit);
                });
                endpoint.Instance(new PipelineConsumer(), consumer =>
                {
                    consumer.UseConcurrencyLimit(ComponentConcurrencyLimit);
                    consumer.UseRateLimit(EndpointRateLimit);
                });
                endpoint.UseTransaction();
                endpoint.UseConcurrencyLimit(EndpointConcurrencyLimit);
                endpoint.UseRateLimit(EndpointRateLimit);
            });
        });
        JsonNode probe = await StartAndProbeAsync(bus);
        IReadOnlyCollection<string> filters = FilterTypes(probe);

        Assert.Contains("transform", filters);
        Assert.Contains("transaction", filters);
        Assert.Contains("retry", filters);
        Assert.Contains("saga", filters);
        Assert.Contains("instance", filters);
        AssertSuperset(
            LimitsOf(probe, "concurrencyLimit"),
            [BusConcurrencyLimit, EndpointConcurrencyLimit, ComponentConcurrencyLimit]);
        int[] consumerConcurrency = LimitsOf(probe, "consumerConcurrency").ToArray();
        Assert.Equal(2, consumerConcurrency.Length);
        Assert.All(consumerConcurrency, limit => Assert.Equal(ComponentConcurrencyLimit, limit));
        AssertSuperset(LimitsOf(probe, "rateLimit"), [BusRateLimit, EndpointRateLimit]);
        Assert.All(IntervalsOf(probe, "rateLimit"), interval => Assert.Equal(TimeSpan.FromSeconds(1), interval));
        Assert.Contains("memory", TextOf(probe, "sagaRepository", "persistence"));
        Assert.Contains(typeof(PipelineConsumer).FullName!, TextOf(probe, "consumer", "type"));
    }

    [Theory]
    [InlineData(ManagedConcurrencyFacet.Endpoint)]
    [InlineData(ManagedConcurrencyFacet.Consumer)]
    [InlineData(ManagedConcurrencyFacet.Message)]
    [InlineData(ManagedConcurrencyFacet.Handler)]
    [InlineData(ManagedConcurrencyFacet.Saga)]
    [RequirementCoverage("REQ-VSB-BUILT-PIPELINE", "concurrency-management-arguments-fail-at-every-public-entry-point")]
    public void ConcurrencyManagementConfiguration_RejectsEveryInvalidArgumentAtEachPublicEntryPoint(
        ManagedConcurrencyFacet facet)
    {
        ArgumentOutOfRangeException concurrencyLimit = Assert.Throws<ArgumentOutOfRangeException>(() =>
            BuildInvalidManagedLimit(facet, 0, useNullManagementEndpoint: false, limiterId: "orders"));
        ArgumentNullException managementEndpoint = Assert.Throws<ArgumentNullException>(() =>
            BuildInvalidManagedLimit(facet, 1, useNullManagementEndpoint: true, limiterId: "orders"));
        ArgumentException limiterId = Assert.Throws<ArgumentException>(() =>
            BuildInvalidManagedLimit(facet, 1, useNullManagementEndpoint: false, limiterId: " "));

        Assert.Equal("concurrencyLimit", concurrencyLimit.ParamName);
        Assert.Equal(0, concurrencyLimit.ActualValue);
        Assert.Equal("managementEndpointConfigurator", managementEndpoint.ParamName);
        Assert.Equal("limiterId", limiterId.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUILT-PIPELINE", "typed-management-command-cannot-limit-its-own-adjustment-path")]
    public void TypedConcurrencyManagement_RejectsACircularManagementCommandPipeline()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            Bus.Factory.CreateUsingInMemory(configuration => configuration.ReceiveEndpoint(
                $"circular-managed-{NewId.NextGuid():N}",
                endpoint => endpoint.Consumer<CircularManagementConsumer>(consumer =>
                    consumer.Message<SetConcurrencyLimit>(message =>
                        message.UseConcurrencyLimit(2, endpoint, "circular"))))));

        Assert.Contains(nameof(SetConcurrencyLimit), exception.Message, StringComparison.Ordinal);
        Assert.Contains("cannot be protected", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ManagedConcurrencyFacet.Endpoint)]
    [InlineData(ManagedConcurrencyFacet.Consumer)]
    [InlineData(ManagedConcurrencyFacet.Message)]
    [InlineData(ManagedConcurrencyFacet.Handler)]
    [InlineData(ManagedConcurrencyFacet.Saga)]
    [RequirementCoverage("REQ-VSB-BUILT-PIPELINE", "concurrency-management-end-to-end-at-every-public-entry-point")]
    public async Task ConcurrencyManagementCommand_ChangesTheExactProtectedPipelineAtEachPublicEntryPointAsync(
        ManagedConcurrencyFacet facet)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string queueName = $"managed-{facet.ToString().ToLowerInvariant()}-{NewId.NextGuid():N}";
        string limiterId = $"{facet}-limiter";
        Guid probeId = NewId.NextGuid();
        var tracker = new ManagedConcurrencyTracker();
        Assert.True(ManagedTrackers.TryAdd(probeId, tracker));
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
            configuration.ReceiveEndpoint(queueName, endpoint => ConfigureManagedLimit(endpoint, facet, 1, endpoint, limiterId)));
        var started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Guid secondMessageId = NewId.NextGuid();
            using ConnectHandle arrivalObserver = bus.ConnectReceiveObserver(
                new MessageArrivalObserver(secondMessageId, tracker.SecondArrived));

            await endpoint.SendAsync(new ManagedPipelineMessage(NewId.NextGuid(), probeId), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await tracker.FirstEntered.Task.WaitAsync(timeout, cancellationToken);
            await endpoint.SendAsync(
                    new ManagedPipelineMessage(NewId.NextGuid(), probeId),
                    context => context.MessageId = secondMessageId,
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await tracker.SecondArrived.Task.WaitAsync(timeout, cancellationToken);
            Assert.False(tracker.SecondEntered.Task.IsCompleted);

            IRequestClient<SetConcurrencyLimit> client = bus.CreateRequestClient<SetConcurrencyLimit>(
                new Uri($"queue:{queueName}"), TimeSpan.FromSeconds(5));
            Response<ConcurrencyLimitUpdated> response = await client.Advanced().GetResponseAsync<ConcurrencyLimitUpdated>(
                    new ManagedConcurrencyLimitCommand(2, DateTimeOffset.UtcNow, limiterId),
                    cancellationToken: cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            Assert.Equal(2, response.Message.ConcurrencyLimit);
            Assert.Equal(limiterId, response.Message.LimiterId);
            await tracker.SecondEntered.Task.WaitAsync(timeout, cancellationToken);
            tracker.Release.TrySetResult();
            await tracker.Completed.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            tracker.Release.TrySetResult();
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

            ManagedTrackers.TryRemove(probeId, out _);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUILT-PIPELINE", "endpoint-concurrency-limit-protects-batch-consumers")]
    public async Task EndpointConcurrencyLimit_AppliesItsSharedBudgetToBatchConsumersAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string queueName = $"limited-batch-{NewId.NextGuid():N}";
        Guid probeId = NewId.NextGuid();
        var tracker = new ManagedConcurrencyTracker();
        var admissionProbe = new BatchAdmissionProbeFilter();
        Assert.True(ManagedTrackers.TryAdd(probeId, tracker));
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ReceiveEndpoint(queueName, endpoint =>
        {
            endpoint.UseConcurrencyLimit(1);
            ConfigureManagedBatch(endpoint, admissionProbe);
        }));
        var started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Guid secondMessageId = NewId.NextGuid();
            using ConnectHandle arrivalObserver = bus.ConnectReceiveObserver(
                new MessageArrivalObserver(secondMessageId, tracker.SecondArrived, tracker.SecondReceived));

            await endpoint.SendAsync(new ManagedPipelineMessage(NewId.NextGuid(), probeId), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await tracker.FirstEntered.Task.WaitAsync(timeout, cancellationToken);
            await endpoint.SendAsync(
                    new ManagedPipelineMessage(NewId.NextGuid(), probeId),
                    context => context.MessageId = secondMessageId,
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await tracker.SecondArrived.Task.WaitAsync(timeout, cancellationToken);
            await admissionProbe.SecondDownstreamAttempted.Task.WaitAsync(timeout, cancellationToken);
            Assert.False(tracker.SecondEntered.Task.IsCompleted);

            tracker.Release.TrySetResult();
            await tracker.SecondEntered.Task.WaitAsync(timeout, cancellationToken);
            await tracker.Completed.Task.WaitAsync(timeout, cancellationToken);
            await tracker.SecondReceived.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            tracker.Release.TrySetResult();
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

            ManagedTrackers.TryRemove(probeId, out _);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUILT-PIPELINE", "endpoint-concurrency-limit-projects-once-to-batch-pipeline")]
    public async Task EndpointConcurrencyLimit_ProjectsExactlyOneFilterIntoABatchPipelineAsync()
    {
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ReceiveEndpoint(
            $"limited-batch-probe-{NewId.NextGuid():N}",
            endpoint =>
            {
                endpoint.UseConcurrencyLimit(1);
                ConfigureManagedBatch(endpoint);
            }));

        JsonNode probe = await StartAndProbeAsync(bus);

        Assert.Equal([1], LimitsOf(probe, "concurrencyLimit"));
    }

    private static readonly ConcurrentDictionary<Guid, ManagedConcurrencyTracker> ManagedTrackers = new();

    private static void BuildInvalidManagedLimit(
        ManagedConcurrencyFacet facet,
        int concurrencyLimit,
        bool useNullManagementEndpoint,
        string? limiterId)
    {
        _ = Bus.Factory.CreateUsingInMemory(configuration => configuration.ReceiveEndpoint(
            $"invalid-managed-{NewId.NextGuid():N}",
            endpoint => ConfigureManagedLimit(
                endpoint,
                facet,
                concurrencyLimit,
                useNullManagementEndpoint ? null! : endpoint,
                limiterId)));
    }

    private static void ConfigureManagedLimit(
        IReceiveEndpointConfigurator endpoint,
        ManagedConcurrencyFacet facet,
        int concurrencyLimit,
        IReceiveEndpointConfigurator managementEndpoint,
        string? limiterId)
    {
        switch (facet)
        {
            case ManagedConcurrencyFacet.Endpoint:
                endpoint.UseConcurrencyLimit(concurrencyLimit, managementEndpoint, limiterId);
                endpoint.Consumer<ManagedPipelineConsumer>();
                break;
            case ManagedConcurrencyFacet.Consumer:
                endpoint.Consumer<ManagedPipelineConsumer>(consumer =>
                    consumer.UseConcurrencyLimit(concurrencyLimit, managementEndpoint, limiterId));
                break;
            case ManagedConcurrencyFacet.Message:
                endpoint.Consumer<ManagedPipelineConsumer>(consumer =>
                    consumer.Message<ManagedPipelineMessage>(message =>
                        message.UseConcurrencyLimit(concurrencyLimit, managementEndpoint, limiterId)));
                break;
            case ManagedConcurrencyFacet.Handler:
                endpoint.Handler<ManagedPipelineMessage>(
                    context => ExecuteManagedAsync(context.Message.ProbeId, context.CancellationToken),
                    handler => handler.UseConcurrencyLimit(concurrencyLimit, managementEndpoint, limiterId));
                break;
            case ManagedConcurrencyFacet.Saga:
                endpoint.Saga(
                    new InMemorySagaRepository<ManagedPipelineSaga>(),
                    saga => saga.UseConcurrencyLimit(concurrencyLimit, managementEndpoint, limiterId));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(facet), facet, "Unknown managed-concurrency facet.");
        }
    }

    private static Task ExecuteManagedAsync(Guid probeId, CancellationToken cancellationToken)
    {
        if (!ManagedTrackers.TryGetValue(probeId, out ManagedConcurrencyTracker? tracker))
            throw new InvalidOperationException($"No managed-concurrency tracker is registered for {probeId}.");

        return tracker.ExecuteAsync(cancellationToken);
    }

    private static void ConfigureManagedBatch(
        IReceiveEndpointConfigurator endpoint,
        BatchAdmissionProbeFilter? admissionProbe = null)
    {
        endpoint.Batch<ManagedPipelineMessage>(batch =>
        {
            batch.MessageLimit = 1;
            batch.TimeLimit = TimeSpan.FromMinutes(1);
            batch.ConcurrencyLimit = 2;
            batch.Consumer(
                new DelegateConsumerFactory<ManagedPipelineBatchConsumer>(() => new ManagedPipelineBatchConsumer()),
                consumer => consumer.Message(message =>
                {
                    if (admissionProbe != null)
                        message.UseFilter(admissionProbe);
                }));
        });
    }

    private static async Task<JsonNode> StartAndProbeAsync(IBusControl bus)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            return JsonNode.Parse(JsonSerializer.Serialize(bus.GetProbeResult().Results))!;
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void AssertSuperset(IReadOnlyCollection<int> actual, IEnumerable<int> expected)
    {
        foreach (int value in expected)
            Assert.Contains(value, actual);
    }

    private static IReadOnlyCollection<string> FilterTypes(JsonNode probe) =>
        PropertiesIn(probe)
            .Where(property => property.Key == "filterType")
            .Select(property => property.Value.ToString())
            .ToArray();

    private static IReadOnlyCollection<int> LimitsOf(JsonNode probe, string filterType) =>
        NodesIn(probe)
            .OfType<JsonObject>()
            .Where(node => node["filterType"]?.ToString() == filterType && node["limit"] is not null)
            .Select(node => node["limit"]!.GetValue<int>())
            .ToArray();

    private static IReadOnlyCollection<TimeSpan> IntervalsOf(JsonNode probe, string filterType) =>
        NodesIn(probe)
            .OfType<JsonObject>()
            .Where(node => node["filterType"]?.ToString() == filterType && node["interval"] is not null)
            .Select(node => TimeSpan.Parse(node["interval"]!.ToString(), CultureInfo.InvariantCulture))
            .ToArray();

    private static IReadOnlyCollection<string> TextOf(JsonNode probe, string scope, string key) =>
        Below(probe, scope)
            .Where(property => string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase))
            .Select(property => property.Value.ToString())
            .ToArray();

    private static IEnumerable<KeyValuePair<string, JsonNode>> Below(JsonNode probe, string scope) =>
        PropertiesIn(probe)
            .Where(property => string.Equals(property.Key, scope, StringComparison.Ordinal))
            .Select(property => property.Value)
            .Where(value => value is JsonObject or JsonArray)
            .SelectMany(PropertiesIn);

    private static IEnumerable<JsonNode> NodesIn(JsonNode node)
    {
        yield return node;

        if (node is JsonObject jsonObject)
        {
            foreach ((_, JsonNode? value) in jsonObject)
            {
                if (value is null)
                    continue;

                foreach (JsonNode nested in NodesIn(value))
                    yield return nested;
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (JsonNode? value in jsonArray)
            {
                if (value is null)
                    continue;

                foreach (JsonNode nested in NodesIn(value))
                    yield return nested;
            }
        }
    }

    private static IEnumerable<KeyValuePair<string, JsonNode>> PropertiesIn(JsonNode node) =>
        NodesIn(node)
            .OfType<JsonObject>()
            .SelectMany(jsonObject => jsonObject)
            .Where(property => property.Value is not null)
            .Select(property => new KeyValuePair<string, JsonNode>(property.Key, property.Value!));

    public sealed class PipelineMessage : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class PipelineConsumer : IConsumer<PipelineMessage>
    {
        public Task ConsumeAsync(ConsumeContext<PipelineMessage> context) => Task.CompletedTask;
    }

    private sealed class PipelineSaga : ISaga, InitiatedBy<PipelineMessage>
    {
        public PipelineSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<PipelineMessage> context) => Task.CompletedTask;
    }

    public enum ManagedConcurrencyFacet
    {
        Endpoint,
        Consumer,
        Message,
        Handler,
        Saga,
    }

    private sealed record ManagedPipelineMessage(Guid CorrelationId, Guid ProbeId) : CorrelatedBy<Guid>;

    private sealed record ManagedConcurrencyLimitCommand(
        int ConcurrencyLimit,
        DateTimeOffset? Timestamp,
        string? LimiterId) : SetConcurrencyLimit;

    private sealed class ManagedPipelineConsumer : IConsumer<ManagedPipelineMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ManagedPipelineMessage> context) =>
            ExecuteManagedAsync(context.Message.ProbeId, context.CancellationToken);
    }

    private sealed class CircularManagementConsumer : IConsumer<SetConcurrencyLimit>
    {
        public Task ConsumeAsync(ConsumeContext<SetConcurrencyLimit> context) => Task.CompletedTask;
    }

    private sealed class ManagedPipelineSaga : ISaga, InitiatedBy<ManagedPipelineMessage>
    {
        public ManagedPipelineSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<ManagedPipelineMessage> context) =>
            ExecuteManagedAsync(context.Message.ProbeId, context.CancellationToken);
    }

    private sealed class ManagedPipelineBatchConsumer : IConsumer<Batch<ManagedPipelineMessage>>
    {
        public Task ConsumeAsync(ConsumeContext<Batch<ManagedPipelineMessage>> context)
        {
            ConsumeContext<ManagedPipelineMessage> message = Assert.Single(context.Message);
            return ExecuteManagedAsync(message.Message.ProbeId, context.CancellationToken);
        }
    }

    private sealed class BatchAdmissionProbeFilter : IFilter<ConsumeContext<Batch<ManagedPipelineMessage>>>
    {
        private int _attempts;

        public TaskCompletionSource SecondDownstreamAttempted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task SendAsync(
            ConsumeContext<Batch<ManagedPipelineMessage>> context,
            IPipe<ConsumeContext<Batch<ManagedPipelineMessage>>> next)
        {
            Task downstream = next.SendAsync(context);
            if (Interlocked.Increment(ref _attempts) == 2)
                SecondDownstreamAttempted.TrySetResult();

            await downstream.ConfigureAwait(false);
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class ManagedConcurrencyTracker
    {
        private int _completed;
        private int _entered;

        public TaskCompletionSource Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource FirstEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SecondEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SecondArrived { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SecondReceived { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            int entered = Interlocked.Increment(ref _entered);
            if (entered == 1)
                FirstEntered.TrySetResult();
            else if (entered == 2)
                SecondEntered.TrySetResult();

            try
            {
                await Release.Task.WaitAsync(
                    TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value,
                    cancellationToken);
            }
            finally
            {
                if (Interlocked.Increment(ref _completed) == 2)
                    Completed.TrySetResult();
            }
        }
    }

    private sealed class MessageArrivalObserver(
        Guid messageId,
        TaskCompletionSource arrived,
        TaskCompletionSource? received = null) : IReceiveObserver
    {
        public Task PreReceiveAsync(ReceiveContext context)
        {
            if (context.GetMessageId() == messageId)
                arrived.TrySetResult();

            return Task.CompletedTask;
        }

        public Task PostReceiveAsync(ReceiveContext context)
        {
            if (context.GetMessageId() == messageId)
                received?.TrySetResult();

            return Task.CompletedTask;
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class
            => Task.CompletedTask;

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception) => Task.CompletedTask;
    }
}
