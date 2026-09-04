using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class ServiceInstanceEndpointTests
{
    private const string InstancePrefix = "Instance_";

    [Fact]
    [RequirementCoverage("REQ-VSB-SERVICE-INSTANCE-ENDPOINTS", "instance-isolation-with-shared-consumer-endpoint")]
    public async Task ServiceInstances_AddDistinctInstanceEndpointsAndRetainTheSharedConsumerEndpointAsync()
    {
        string[] plain = await EndpointNamesAsync(useServiceInstance: false);
        string[] first = await EndpointNamesAsync(useServiceInstance: true);
        string[] second = await EndpointNamesAsync(useServiceInstance: true);
        string firstInstance = Assert.Single(first, IsInstance);
        string secondInstance = Assert.Single(second, IsInstance);
        string sharedEndpoint = nameof(ServiceInstanceMessage).Replace("Message", string.Empty, StringComparison.Ordinal);

        Assert.DoesNotContain(plain, IsInstance);
        Assert.StartsWith(InstancePrefix, firstInstance, StringComparison.Ordinal);
        Assert.StartsWith(InstancePrefix, secondInstance, StringComparison.Ordinal);
        Assert.NotEqual(firstInstance, secondInstance);
        Assert.Contains(sharedEndpoint, first);
        Assert.Contains(sharedEndpoint, second);
    }

    private static bool IsInstance(string name) =>
        name.StartsWith(InstancePrefix, StringComparison.Ordinal);

    private static async Task<string[]> EndpointNamesAsync(bool useServiceInstance)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        await using ServiceProvider provider = services
            .AddViciOneServiceBus(configuration =>
            {
                configuration.AddConsumer<ServiceInstanceConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    if (useServiceInstance)
                        bus.ConfigureServiceInstanceEndpoints(context);
                    else
                        bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            JsonNode probe = JsonNode.Parse(JsonSerializer.Serialize(bus.GetProbeResult().Results))!;
            return probe["bus"]!["host"]!["receiveEndpoint"]!.AsArray()
                .Select(endpoint => endpoint!["name"]!.GetValue<string>())
                .Order(StringComparer.Ordinal)
                .ToArray();
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record ServiceInstanceMessage(string Value);

    private sealed class ServiceInstanceConsumer : IConsumer<ServiceInstanceMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ServiceInstanceMessage> context) => Task.CompletedTask;
    }
}
