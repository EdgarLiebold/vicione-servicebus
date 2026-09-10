using global::Azure;
using global::Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.AzureServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests;

public sealed class AzureServiceBusTestHarnessCleanupTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TEST-HARNESS", "real-namespace-queue-and-topic-cleanup")]
    public async Task CleanAsync_RemovesEveryQueueAndTopicFromARealNamespaceAsync()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("harness-clean");
        ServiceBusAdministrationClient administrationClient = fixture.CreateAdministrationClient();
        string queueName = fixture.Name("queue");
        string topicName = fixture.Name("topic");
        using CancellationTokenSource timeout = fixture.OperationCancellation();
        using var harness = new TestableHarness(administrationClient);

        try
        {
            await administrationClient.CreateQueueAsync(queueName, timeout.Token);
            await administrationClient.CreateTopicAsync(topicName, timeout.Token);
            Assert.True(await administrationClient.QueueExistsAsync(queueName, timeout.Token));
            Assert.True(await administrationClient.TopicExistsAsync(topicName, timeout.Token));

            await harness.CleanAsync(timeout.Token).WaitAsync(fixture.OperationTimeout, timeout.Token);

            Assert.False(await administrationClient.QueueExistsAsync(queueName, timeout.Token));
            Assert.False(await administrationClient.TopicExistsAsync(topicName, timeout.Token));
        }
        finally
        {
            await fixture.CleanupAsync(administrationClient);
        }
    }

    private sealed class TestableHarness(ServiceBusAdministrationClient administrationClient)
        : AzureServiceBusTestHarness(
            new Uri("sb://namespace.example"),
            new AzureNamedKeyCredential("unused", "unused"))
    {
        protected override ServiceBusAdministrationClient CreateAdministrationClient() => administrationClient;
    }
}
