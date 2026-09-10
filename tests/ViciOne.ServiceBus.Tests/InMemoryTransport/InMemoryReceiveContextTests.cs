using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryReceiveContextTests
{
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
