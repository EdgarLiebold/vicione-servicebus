using System.Text.Json;
using ViciOne.ServiceBus.AzureServiceBusTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests;

public sealed class ServiceBusSessionSagaProbeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SESSION-SAGA", "probe-names-service-bus-session-persistence")]
    public void MessageSessionSagaProbe_NamesItsActualPersistenceOwner()
    {
        var factory = new MessageSessionSagaRepositoryContextFactory<ProbedSaga>(null!);

        string? persistence = JsonSerializer.SerializeToElement(
                factory.GetProbeResult(TestContext.Current.CancellationToken).Results)
            .GetProperty("persistence")
            .GetString();

        Assert.Equal("azure-service-bus-message-session", persistence);
    }

    public sealed class ProbedSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
