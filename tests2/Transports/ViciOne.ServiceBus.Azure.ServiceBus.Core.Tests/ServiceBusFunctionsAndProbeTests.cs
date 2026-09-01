using System.Text.Json;
using System.Text.Json.Nodes;
using ViciOne.ServiceBus.AzureServiceBusTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests;

public sealed class ServiceBusSessionSagaProbeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROBE", "configured-bus-renders-provider-endpoint-details")]
    public void ConfiguredBus_ProbeRendersProviderEndpointDetails()
    {
        IBusControl bus = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.OverrideDefaultBusEndpointQueueName("probe-bus");
            configuration.ReceiveEndpoint("probe-input", endpoint => endpoint.PrefetchCount = 17);
        });

        JsonNode probe = JsonSerializer.SerializeToNode(
            bus.GetProbeResult(TestContext.Current.CancellationToken),
            Serialization.SystemTextJsonMessageSerializer.Options)!;
        JsonNode receiveEndpoints = probe["results"]!["bus"]!["host"]!["receiveEndpoint"]!;
        JsonObject[] endpoints = receiveEndpoints is JsonArray array
            ? array.Select(value => value!.AsObject()).ToArray()
            : [receiveEndpoints.AsObject()];

        Assert.Equal(2, endpoints.Length);
        JsonObject configuredEndpoint = Assert.Single(
            endpoints,
            endpoint => endpoint.ToJsonString().Contains("probe-input", StringComparison.Ordinal));
        Assert.Equal(17, configuredEndpoint["receiveTransport"]?["prefetchCount"]?.GetValue<int>());
    }

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
