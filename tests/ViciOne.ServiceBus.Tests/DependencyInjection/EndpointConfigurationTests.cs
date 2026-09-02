using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class EndpointConfigurationTests
{
    private const string EndpointName = "native-endpoint-configuration";
    private const int BusPrefetchCount = 427;

    [Theory]
    [InlineData(ConfigurationShape.BusOnly, BusPrefetchCount, null)]
    [InlineData(ConfigurationShape.EndpointInheritsBus, BusPrefetchCount, null)]
    [InlineData(ConfigurationShape.EndpointOverridesBus, 351, null)]
    [InlineData(ConfigurationShape.DefinitionSetsConcurrency, 120, 100)]
    [InlineData(ConfigurationShape.DefinitionSetsBoth, 351, 100)]
    [InlineData(ConfigurationShape.RegistrationSetsConcurrency, 120, 100)]
    [InlineData(ConfigurationShape.EmptyDefinitionInheritsBus, BusPrefetchCount, null)]
    [RequirementCoverage("REQ-VSB-DI-ENDPOINT-CONFIGURATION", "prefetch-and-concurrency-precedence-matrix")]
    public async Task ProbeReportsTheExactEffectivePrefetchAndConcurrencyValues(
        ConfigurationShape shape,
        int expectedPrefetchCount,
        int? expectedConcurrentMessageLimit)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = CreateProvider(shape);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            JsonNode probe = JsonNode.Parse(JsonSerializer.Serialize(
                provider.GetRequiredService<IBusControl>().GetProbeResult(cancellationToken).Results))!;
            EndpointProbe endpoint = ReadEndpoint(probe, shape);

            Assert.Equal(expectedPrefetchCount, endpoint.PrefetchCount);
            Assert.Equal(expectedConcurrentMessageLimit, endpoint.ConcurrentMessageLimit);
            Assert.Equal(shape == ConfigurationShape.BusOnly, endpoint.IsBusEndpoint);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DI-ENDPOINT-CONFIGURATION", "validated-container-scopes-start-and-stop")]
    public async Task EndpointRegistrationBuildsStartsAndStopsWithScopeValidationEnabled()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<EndpointConsumer>()
                    .Endpoint(endpoint =>
                    {
                        endpoint.Name = EndpointName;
                        endpoint.ConcurrentMessageLimit = 1;
                    });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        Assert.Same(provider.GetRequiredService<IBus>(), harness.Bus);
        await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

        BusHealthResult health = provider.GetRequiredService<IBusControl>().CheckHealth();
        Assert.Equal(BusHealthStatus.Unhealthy, health.Status);
    }

    private static ServiceProvider CreateProvider(ConfigurationShape shape)
    {
        TimeSpan timeout = OperationTimeout();
        return new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                RegisterConsumer(configuration, shape);
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.PrefetchCount = BusPrefetchCount;
                    switch (shape)
                    {
                        case ConfigurationShape.BusOnly:
                            break;
                        case ConfigurationShape.EndpointInheritsBus:
                            bus.ReceiveEndpoint(EndpointName, _ => { });
                            break;
                        case ConfigurationShape.EndpointOverridesBus:
                            bus.ReceiveEndpoint(EndpointName, endpoint => endpoint.PrefetchCount = 351);
                            break;
                        default:
                            bus.ConfigureEndpoints(context);
                            break;
                    }
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }

    private static void RegisterConsumer(
        IBusRegistrationConfigurator configuration,
        ConfigurationShape shape)
    {
        switch (shape)
        {
            case ConfigurationShape.DefinitionSetsConcurrency:
                configuration.AddConsumer<EndpointConsumer, ConcurrentDefinition>();
                break;
            case ConfigurationShape.DefinitionSetsBoth:
                configuration.AddConsumer<EndpointConsumer, CompleteDefinition>();
                break;
            case ConfigurationShape.RegistrationSetsConcurrency:
                configuration.AddConsumer<EndpointConsumer>()
                    .Endpoint(endpoint =>
                    {
                        endpoint.Name = EndpointName;
                        endpoint.ConcurrentMessageLimit = 100;
                    });
                break;
            case ConfigurationShape.EmptyDefinitionInheritsBus:
                configuration.AddConsumer<EndpointConsumer, EmptyDefinition>();
                break;
        }
    }

    private static EndpointProbe ReadEndpoint(JsonNode root, ConfigurationShape shape)
    {
        EndpointProbe[] endpoints = NodesIn(root)
            .OfType<JsonObject>()
            .Where(node => node["name"] is not null && node["receiveTransport"] is JsonObject)
            .Select(node =>
            {
                var transport = (JsonObject)node["receiveTransport"]!;
                string name = node["name"]!.GetValue<string>();
                return new EndpointProbe(
                    name,
                    transport["prefetchCount"]!.GetValue<int>(),
                    transport["concurrentMessageLimit"]?.GetValue<int>(),
                    name.Contains("_bus_", StringComparison.OrdinalIgnoreCase));
            })
            .ToArray();

        return shape switch
        {
            ConfigurationShape.BusOnly => Assert.Single(endpoints, endpoint => endpoint.IsBusEndpoint),
            ConfigurationShape.DefinitionSetsConcurrency or ConfigurationShape.EmptyDefinitionInheritsBus =>
                Assert.Single(endpoints, endpoint => !endpoint.IsBusEndpoint),
            _ => Assert.Single(endpoints, endpoint => endpoint.Name == EndpointName),
        };
    }

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

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum ConfigurationShape
    {
        BusOnly,
        EndpointInheritsBus,
        EndpointOverridesBus,
        DefinitionSetsConcurrency,
        DefinitionSetsBoth,
        RegistrationSetsConcurrency,
        EmptyDefinitionInheritsBus,
    }

    public sealed record EndpointMessage;

    public sealed class EndpointConsumer : IConsumer<EndpointMessage>
    {
        public Task Consume(ConsumeContext<EndpointMessage> context) => Task.CompletedTask;
    }

    private sealed class ConcurrentDefinition : ConsumerDefinition<EndpointConsumer>
    {
        public ConcurrentDefinition()
        {
            ConcurrentMessageLimit = 100;
        }
    }

    private sealed class CompleteDefinition : ConsumerDefinition<EndpointConsumer>
    {
        public CompleteDefinition()
        {
            Endpoint(endpoint =>
            {
                endpoint.Name = EndpointConfigurationTests.EndpointName;
                endpoint.PrefetchCount = 351;
                endpoint.ConcurrentMessageLimit = 100;
            });
        }
    }

    private sealed class EmptyDefinition : ConsumerDefinition<EndpointConsumer>;

    private sealed record EndpointProbe(
        string Name,
        int PrefetchCount,
        int? ConcurrentMessageLimit,
        bool IsBusEndpoint);
}
