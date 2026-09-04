using System.Text.Json;
using System.Text.Json.Nodes;
using ViciOne.ServiceBus.Saga;
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
                    saga.UseConcurrentMessageLimit(ComponentConcurrencyLimit);
                    saga.UseRateLimit(BusRateLimit);
                });
                endpoint.Consumer<PipelineConsumer>(consumer =>
                {
                    consumer.UseConcurrentMessageLimit(ComponentConcurrencyLimit);
                    consumer.UseRateLimit(EndpointRateLimit);
                });
                endpoint.Instance(new PipelineConsumer(), consumer =>
                {
                    consumer.UseConcurrentMessageLimit(ComponentConcurrencyLimit);
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
        Assert.Contains("memory", TextOf(probe, "sagaRepository", "persistence"));
        Assert.Contains(typeof(PipelineConsumer).FullName!, TextOf(probe, "consumer", "type"));
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
}
