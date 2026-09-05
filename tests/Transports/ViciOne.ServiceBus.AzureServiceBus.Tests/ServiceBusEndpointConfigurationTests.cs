using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusEndpointConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-CONFIGURATION", "bus-and-endpoint-prefetch-precedence")]
    public void BusAndEndpointPrefetch_UseTheExactConfiguredPrecedence()
    {
        IBusControl inherited = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.PrefetchCount = 427;
            configuration.ReceiveEndpoint("inherited", _ => { });
        });
        IBusControl overridden = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.PrefetchCount = 427;
            configuration.ReceiveEndpoint("overridden", endpoint => endpoint.PrefetchCount = 351);
        });

        Assert.Equal([427, 427], PrefetchCounts(inherited));
        Assert.Equal([351, 427], PrefetchCounts(overridden));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-CONFIGURATION", "consumer-definition-and-endpoint-definition-precedence")]
    public async Task ConsumerDefinitions_ProjectExactPrefetchAndConcurrencyAsync()
    {
        await using ServiceProvider consumerProvider = CreateProvider<PingConsumerDefinition>();
        await using ServiceProvider endpointProvider = CreateProvider<EndpointPingConsumerDefinition>();
        await using ServiceProvider emptyProvider = CreateProvider<EmptyPingConsumerDefinition>();

        Assert.Equal((427, 0, 427), Probe(consumerProvider.GetRequiredService<IBusControl>()));
        Assert.Equal((351, 100, 427), Probe(endpointProvider.GetRequiredService<IBusControl>()));
        Assert.Equal((427, 0, 427), Probe(emptyProvider.GetRequiredService<IBusControl>()));
    }

    static ServiceProvider CreateProvider<TDefinition>()
        where TDefinition : class, IConsumerDefinition<PingConsumer>
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<PingConsumer, TDefinition>();
            configuration.UsingAzureServiceBus((context, bus) =>
            {
                bus.PrefetchCount = 427;
                bus.ConfigureEndpoints(context);
            });
        });
        return services.BuildServiceProvider(validateScopes: true);
    }

    static int[] PrefetchCounts(IBusControl bus)
    {
        JsonNode probe = JsonSerializer.SerializeToNode(
            bus.GetProbeResult(TestContext.Current.CancellationToken),
            ServiceBusMetadataJson.Options)!;
        return ReceiveEndpoints(probe)
            .Select(endpoint => endpoint["receiveTransport"]!["prefetchCount"]!.GetValue<int>())
            .ToArray();
    }

    static (int Prefetch, int Concurrency, int BusPrefetch) Probe(IBusControl bus)
    {
        JsonObject[] endpoints = ReceiveEndpoints(JsonSerializer.SerializeToNode(
            bus.GetProbeResult(TestContext.Current.CancellationToken),
            ServiceBusMetadataJson.Options)!).ToArray();
        JsonObject configured = endpoints[0];
        JsonObject busEndpoint = endpoints[1];
        return (
            configured["receiveTransport"]!["prefetchCount"]!.GetValue<int>(),
            configured["receiveTransport"]!["concurrentMessageLimit"]?.GetValue<int>() ?? 0,
            busEndpoint["receiveTransport"]!["prefetchCount"]!.GetValue<int>());
    }

    static IEnumerable<JsonObject> ReceiveEndpoints(JsonNode probe)
    {
        JsonNode endpoints = probe["results"]!["bus"]!["host"]!["receiveEndpoint"]!;
        return endpoints is JsonArray array
            ? array.Select(x => x!.AsObject())
            : [endpoints.AsObject()];
    }

    public sealed class PingMessage;

    public sealed class PingConsumer : IConsumer<PingMessage>
    {
        public Task ConsumeAsync(ConsumeContext<PingMessage> context) => Task.CompletedTask;
    }

    public sealed class PingConsumerDefinition : ConsumerDefinition<PingConsumer>
    {
        public PingConsumerDefinition() => ConcurrentMessageLimit = 100;
    }

    public sealed class EndpointPingConsumerDefinition : ConsumerDefinition<PingConsumer>
    {
        public EndpointPingConsumerDefinition()
        {
            Endpoint(endpoint =>
            {
                endpoint.PrefetchCount = 351;
                endpoint.ConcurrentMessageLimit = 100;
            });
        }
    }

    public sealed class EmptyPingConsumerDefinition : ConsumerDefinition<PingConsumer>;
}
