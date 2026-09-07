using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Introspection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests;

public sealed class ActiveMqEndpointConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-ENDPOINT-CONFIGURATION", "prefetch-and-concurrency-precedence")]
    public void ConcurrencyAndPrefetch_ResolveFromEndpointAndBusSettings()
    {
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var bus = new ActiveMqBusConfiguration(topology);
        bus.Transport.Configurator.PrefetchCount = 427;

        var inherited = new ActiveMqQueueReceiveSettings(bus.CreateEndpointConfiguration(false), "inherited", true, false);
        Assert.Equal(427, inherited.PrefetchCount);
        Assert.Equal(427, inherited.ConcurrentMessageLimit);

        IActiveMqEndpointConfiguration directOverride = bus.CreateEndpointConfiguration(false);
        directOverride.Transport.Configurator.PrefetchCount = 351;
        var directlyOverridden = new ActiveMqQueueReceiveSettings(directOverride, "direct-override", true, false);
        Assert.Equal(351, directlyOverridden.PrefetchCount);
        Assert.Equal(351, directlyOverridden.ConcurrentMessageLimit);

        ActiveMqHostConfiguration host = Assert.IsType<ActiveMqHostConfiguration>(bus.HostConfiguration);
        ActiveMqQueueReceiveSettings calculated = ApplyDefinition(bus, host, "calculated", prefetchCount: null, concurrentMessageLimit: 100);
        Assert.Equal(120, calculated.PrefetchCount);
        Assert.Equal(100, calculated.ConcurrentMessageLimit);

        ActiveMqQueueReceiveSettings explicitlyConfigured = ApplyDefinition(
            bus,
            host,
            "explicitly-configured",
            prefetchCount: 351,
            concurrentMessageLimit: 100);
        Assert.Equal(351, explicitlyConfigured.PrefetchCount);
        Assert.Equal(100, explicitlyConfigured.ConcurrentMessageLimit);

        bus.Transport.Configurator.ConcurrentMessageLimit = 120;
        var inheritedConcurrency = new ActiveMqQueueReceiveSettings(
            bus.CreateEndpointConfiguration(false),
            "inherited-concurrency",
            true,
            false);
        Assert.Equal(427, inheritedConcurrency.PrefetchCount);
        Assert.Equal(120, inheritedConcurrency.ConcurrentMessageLimit);

        IBusControl configuredBus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            configurator.Host(new Uri("activemq://localhost:61616"), _ => { });
            configurator.PrefetchCount = 427;
            configurator.ReceiveEndpoint(new TestEndpointDefinition("definition-derived", null, 100));
            configurator.ReceiveEndpoint(new TestEndpointDefinition("definition-explicit", 351, 100));
            configurator.ReceiveEndpoint("inherited", _ => { });
            configurator.ReceiveEndpoint("direct-override", endpoint => endpoint.PrefetchCount = 351);
        });

        AssertEndpointProbe(configuredBus, "definition-derived", expectedPrefetch: 120, expectedConcurrency: 100);
        AssertEndpointProbe(configuredBus, "definition-explicit", expectedPrefetch: 351, expectedConcurrency: 100);
        AssertEndpointProbe(configuredBus, "inherited", expectedPrefetch: 427, expectedConcurrency: 427);
        AssertEndpointProbe(configuredBus, "direct-override", expectedPrefetch: 351, expectedConcurrency: 351);
    }

    private static ActiveMqQueueReceiveSettings ApplyDefinition(
        ActiveMqBusConfiguration bus,
        ActiveMqHostConfiguration host,
        string name,
        int? prefetchCount,
        int? concurrentMessageLimit)
    {
        IActiveMqEndpointConfiguration endpoint = bus.CreateEndpointConfiguration(false);
        var settings = new ActiveMqQueueReceiveSettings(endpoint, $"definition-{prefetchCount}-{concurrentMessageLimit}", true, false);
        var configuration = new ActiveMqReceiveEndpointConfiguration(host, settings, endpoint);

        host.ApplyEndpointDefinition(configuration, new TestEndpointDefinition(name, prefetchCount, concurrentMessageLimit));
        return settings;
    }

    private static void AssertEndpointProbe(
        IBusControl bus,
        string entityName,
        int expectedPrefetch,
        int expectedConcurrency)
    {
        IProbeResult probe = bus.GetProbeResult(TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, object> busScope = GetScope(probe.Results, "bus");
        IReadOnlyDictionary<string, object> hostScope = GetScope(busScope, "host");
        object endpointValue = Assert.Contains("receiveEndpoint", hostScope);
        IEnumerable<IReadOnlyDictionary<string, object>> endpoints = endpointValue switch
        {
            IReadOnlyDictionary<string, object> single => [single],
            IEnumerable<IReadOnlyDictionary<string, object>> multiple => multiple,
            _ => throw new Xunit.Sdk.XunitException(
                $"The receiveEndpoint probe node has unsupported type '{endpointValue.GetType()}'."),
        };
        IReadOnlyDictionary<string, object> transport = Assert.Single(
            endpoints.Select(endpoint => GetScope(endpoint, "receiveTransport")),
            candidate => entityName.Equals(
                Assert.IsType<string>(Assert.Contains("entityName", candidate)),
                StringComparison.Ordinal));

        Assert.Equal(expectedPrefetch, Assert.IsType<int>(Assert.Contains("prefetchCount", transport)));
        Assert.Equal(expectedConcurrency, Assert.IsType<int>(Assert.Contains("concurrentMessageLimit", transport)));
    }

    private static IReadOnlyDictionary<string, object> GetScope(IReadOnlyDictionary<string, object> parent, string key) =>
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(Assert.Contains(key, parent));

    private sealed class TestEndpointDefinition(string name, int? prefetchCount, int? concurrentMessageLimit) : IEndpointDefinition
    {
        public bool IsTemporary => false;
        public int? PrefetchCount => prefetchCount;
        public int? ConcurrentMessageLimit => concurrentMessageLimit;
        public bool ConfigureConsumeTopology => true;

        public string GetEndpointName(IEndpointNameFormatter formatter) => name;

        public void Configure<T>(T configurator, IRegistrationContext? context = null)
            where T : IReceiveEndpointConfigurator
        {
        }
    }
}
