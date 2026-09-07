using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Introspection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class JobServiceEndpointConfigurationTests
{
    private static readonly string[] JobEndpoints = ["Job", "JobAttempt", "JobType"];

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ENDPOINT-CONFIGURATION", "containerless-outbox-without-scope")]
    public void ContainerlessConfiguration_AppliesTheOutboxWithoutInventingAScope()
    {
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration =>
        {
            configuration.ServiceInstance(instance => instance.ConfigureJobServiceEndpoints());
        });

        IReadOnlyDictionary<string, HashSet<string>> filters = FiltersByEndpoint(bus);

        foreach (string endpoint in JobEndpoints)
        {
            Assert.Contains(endpoint, filters.Keys);
            HashSet<string> endpointFilters = filters[endpoint];
            Assert.Contains("outbox", endpointFilters);
            Assert.DoesNotContain("scope", endpointFilters);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-ENDPOINT-CONFIGURATION", "registration-context-outbox-on-all-job-sagas")]
    public async Task RegistrationContext_AppliesTheOutboxToEveryJobSagaEndpointAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.SetInMemorySagaRepositoryProvider();
                configuration.AddJobSagaStateMachines();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ConfigureDelayedMessageScheduler();
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
            });

        IReadOnlyDictionary<string, HashSet<string>> filters =
            FiltersByEndpoint(provider.GetRequiredService<IBusControl>());

        foreach (string endpoint in JobEndpoints)
        {
            Assert.Contains(endpoint, filters.Keys);
            HashSet<string> endpointFilters = filters[endpoint];
            Assert.Contains("outbox", endpointFilters);
        }
    }

    private static Dictionary<string, HashSet<string>> FiltersByEndpoint(IBusControl bus)
    {
        IProbeResult probe = bus.GetProbeResult();
        IReadOnlyDictionary<string, object> busScope = Scope(probe.Results, "bus");
        IReadOnlyDictionary<string, object> hostScope = Scope(busScope, "host");
        object endpointValue = Assert.Contains("receiveEndpoint", hostScope);
        IEnumerable<IReadOnlyDictionary<string, object>> endpoints = endpointValue switch
        {
            IReadOnlyDictionary<string, object> single => [single],
            IEnumerable<IReadOnlyDictionary<string, object>> multiple => multiple,
            _ => throw new Xunit.Sdk.XunitException(
                $"The receiveEndpoint probe node has unsupported type '{endpointValue.GetType()}'."),
        };

        return endpoints.ToDictionary(
            endpoint => Assert.IsType<string>(Assert.Contains("name", endpoint)),
            endpoint => FilterTypes(endpoint, new HashSet<string>(StringComparer.Ordinal)));
    }

    private static IReadOnlyDictionary<string, object> Scope(IReadOnlyDictionary<string, object> parent, string key) =>
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(Assert.Contains(key, parent));

    private static HashSet<string> FilterTypes(object node, HashSet<string> found)
    {
        switch (node)
        {
            case IReadOnlyDictionary<string, object> dictionary:
                foreach ((string name, object value) in dictionary)
                {
                    if (name == "filterType" && value is string filterType)
                        found.Add(filterType);
                    else
                        FilterTypes(value, found);
                }

                break;
            case IEnumerable<object> sequence:
                foreach (object item in sequence)
                    FilterTypes(item, found);

                break;
        }

        return found;
    }
}
