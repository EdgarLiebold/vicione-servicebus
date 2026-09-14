using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Runtime;
using ViciOne.ServiceBus.InMemoryTransport.Topology;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryReceiveContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-IN-MEMORY-RECEIVE-CONTEXT", "runtime-context-owns-fabric-and-rejects-external-agents")]
    public void RuntimeContext_ExposesItsOwnedFabricAndExactUnsupportedOperations()
    {
        var topology = new InMemoryTopologyConfiguration(InMemoryBus.CreateMessageTopology());
        var bus = new InMemoryBusConfiguration(topology, new Uri("loopback://localhost/"));
        IInMemoryReceiveEndpointConfiguration configuration = bus.HostConfiguration
            .CreateReceiveEndpointConfiguration("runtime-context", null);
        var context = Assert.IsType<InMemoryReceiveEndpointContext>(
            configuration.CreateReceiveEndpointContext());
        var expected = new InvalidOperationException("transport failure");

        Assert.Same(bus.HostConfiguration.TransportProvider, context.TransportContext);
        Assert.Same(bus.HostConfiguration.TransportProvider.MessageFabric, context.MessageFabric);
        Assert.Throws<NotSupportedException>(() => context.AddSendAgent(null!));
        Assert.Throws<NotSupportedException>(() => context.AddConsumeAgent(null!));
        Assert.Same(expected, context.ConvertException(expected, "receive failed"));
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() =>
            context.ConvertException(null!, "receive failed")).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-IN-MEMORY-RECEIVE-CONTEXT", "required-constructor-dependencies")]
    public void Constructor_RejectsMissingTransportDependencies()
    {
        ArgumentNullException missingMessage = Assert.Throws<ArgumentNullException>(() =>
            new InMemoryReceiveContext(null!, null!));
        var message = new InMemoryTransportMessage(
            Guid.Parse("018f6738-7d4a-7b21-86e2-bdfbb3ed5f61"),
            [],
            "application/json");
        ArgumentNullException missingEndpoint = Assert.Throws<ArgumentNullException>(() =>
            new InMemoryReceiveContext(message, null!));

        Assert.Equal("message", missingMessage.ParamName);
        Assert.Equal("receiveEndpointContext", missingEndpoint.ParamName);
    }
}
