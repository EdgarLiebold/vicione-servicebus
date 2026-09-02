using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class HeaderRoundTripTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-OBJECT-HEADER", "complete-interface-round-trip")]
    public async Task ObjectHeader_RoundTripsEveryInterfaceMemberWithoutAliasingTheSenderObject()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        using var harness = new InMemoryTestHarness($"object-header-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        HandlerTestHarness<HeaderMessage> handler = harness.Handler<HeaderMessage>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var original = new ClaimsIdentityValue
        {
            IdentityType = "AAD:Claims",
            IdentityId = 27,
            Claims = ["One", "two", "Three"],
        };

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(
                new HeaderMessage("value"),
                context => context.Headers.Set("Claims-Identity", original),
                cancellationToken);
            ConsumeContext<HeaderMessage> context =
                (await handler.Consumed.SelectAsync(cancellationToken).First()).Context;
            ClaimsIdentity identity = Assert.IsAssignableFrom<ClaimsIdentity>(
                context.Headers.Get<ClaimsIdentity>("Claims-Identity"));

            Assert.NotSame(original, identity);
            Assert.Equal(original.IdentityId, identity.IdentityId);
            Assert.Equal(original.IdentityType, identity.IdentityType);
            Assert.Equal(original.Claims, identity.Claims);
            Assert.NotSame(original.Claims, identity.Claims);
        }
        finally
        {
            await harness.Stop();
        }
    }

    public interface ClaimsIdentity
    {
        string IdentityType { get; }
        int IdentityId { get; }
        string[] Claims { get; }
    }

    private sealed class ClaimsIdentityValue : ClaimsIdentity
    {
        public required string IdentityType { get; init; }
        public required int IdentityId { get; init; }
        public required string[] Claims { get; init; }
    }

    private sealed record HeaderMessage(string Value);
}
