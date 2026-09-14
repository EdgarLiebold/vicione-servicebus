using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusHeaderProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HEADERS", "received-message-is-required")]
    public void Constructor_RejectsAMissingReceivedMessage()
    {
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new ServiceBusHeaderProvider(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HEADERS", "azure-diagnostic-id-projects-to-canonical-activity-id")]
    public void AzureDiagnosticId_ProjectsToTheCanonicalActivityHeader()
    {
        const string activityId = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            properties: new Dictionary<string, object>
            {
                ["Diagnostic-Id"] = activityId,
            });
        var provider = new ServiceBusHeaderProvider(message);

        bool found = provider.TryGetHeader(MessageHeaders.Prefix + "Activity-Id", out object? value);

        Assert.True(found);
        Assert.Equal(activityId, value);
    }
}
