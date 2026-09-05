using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryBusFactoryConfiguratorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-CONFIGURATION", "bus-endpoint-auto-start-follows-configured-value")]
    public void AutoStart_ChangesTheBusEndpointStartupPolicy()
    {
        var topology = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
        var bus = new InMemoryBusConfiguration(topology, new Uri("loopback://localhost/"));
        var configurator = new InMemoryBusFactoryConfigurator(bus);
        ConsumePipeSpecification specification = Assert.IsType<ConsumePipeSpecification>(
            bus.BusEndpointConfiguration.Consume.Specification);

        Assert.True(specification.AutoStart);

        configurator.AutoStart = false;

        Assert.False(specification.AutoStart);
    }
}
