using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class OutboxConsumerIdentityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MULTIBUS-OUTBOX", "stable-owner-and-endpoint-qualified-consumer-id")]
    public void ConsumerIdentity_IsStableAndQualifiedByBusOwnerAndEndpoint()
    {
        OutboxConsumerIdentitySnapshot snapshot = OutboxConsumerIdentityTestDriver.CreateSnapshot();

        Assert.Equal("default", snapshot.DefaultBusKey);
        Assert.Contains(nameof(OutboxConsumerIdentityTestDriver), snapshot.NamedBusKey, StringComparison.Ordinal);
        Assert.NotEqual(snapshot.DefaultId, snapshot.NamedId);
        Assert.Equal(snapshot.NamedId, snapshot.RepeatedNamedId);
        Assert.NotEqual(snapshot.NamedId, snapshot.OtherEndpointId);
    }
}
