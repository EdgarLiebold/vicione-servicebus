using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests;

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
        ActiveMqQueueReceiveSettings calculated = ApplyDefinition(bus, host, prefetchCount: null, concurrentMessageLimit: 100);
        Assert.Equal(120, calculated.PrefetchCount);
        Assert.Equal(100, calculated.ConcurrentMessageLimit);

        ActiveMqQueueReceiveSettings explicitlyConfigured = ApplyDefinition(bus, host, prefetchCount: 351, concurrentMessageLimit: 100);
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
    }

    private static ActiveMqQueueReceiveSettings ApplyDefinition(
        ActiveMqBusConfiguration bus,
        ActiveMqHostConfiguration host,
        int? prefetchCount,
        int? concurrentMessageLimit)
    {
        IActiveMqEndpointConfiguration endpoint = bus.CreateEndpointConfiguration(false);
        var settings = new ActiveMqQueueReceiveSettings(endpoint, $"definition-{prefetchCount}-{concurrentMessageLimit}", true, false);
        var configuration = new ActiveMqReceiveEndpointConfiguration(host, settings, endpoint);

        host.ApplyEndpointDefinition(configuration, new TestEndpointDefinition(prefetchCount, concurrentMessageLimit));
        return settings;
    }

    private sealed class TestEndpointDefinition(int? prefetchCount, int? concurrentMessageLimit) : IEndpointDefinition
    {
        public bool IsTemporary => false;
        public int? PrefetchCount => prefetchCount;
        public int? ConcurrentMessageLimit => concurrentMessageLimit;
        public bool ConfigureConsumeTopology => true;

        public string GetEndpointName(IEndpointNameFormatter formatter) => "endpoint-definition";

        public void Configure<T>(T configurator, IRegistrationContext? context = null)
            where T : IReceiveEndpointConfigurator
        {
        }
    }
}
