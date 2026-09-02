namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.DeployTopologyContracts;
using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AmazonSqsDeployTopologyTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0230", "explicit-types-create-only-included-topics-at-startup")]
    public async Task ExplicitTypes_CreateOnlyIncludedTopicsAtStartup()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("deploy-explicit");
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.DeployPublishTopology = true;
            configurator.Publish<OrderSubmitted>();
            configurator.Publish<PackageShipped>();
        });

        await AssertDeployedTopics(
            fixture,
            bus,
            [typeof(OrderSubmitted), typeof(PackageShipped)]);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0231", "namespace-scan-creates-only-included-topics-at-startup")]
    public async Task NamespaceScan_CreatesOnlyIncludedTopicsAtStartup()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("deploy-namespace");
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.DeployPublishTopology = true;
            configurator.AddPublishMessageTypesFromNamespaceContaining<OrderSubmitted>();
        });

        await AssertDeployedTopics(
            fixture,
            bus,
            [typeof(CustomerEvent), typeof(OrderSubmitted), typeof(OrderEvent), typeof(PackageShipped)]);
    }

    private static async Task AssertDeployedTopics(
        AmazonSqsLocalStack fixture,
        IBusControl bus,
        Type[] includedTypes)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;

            var formatter = new AmazonSqsMessageNameFormatter();
            string[] expected = includedTypes
                .Select(type => $"{fixture.Prefix}_{formatter.GetMessageName(type)}")
                .Order(StringComparer.Ordinal)
                .ToArray();
            string[] actual = await fixture.ListOwnedTopicNames(cancellationToken);
            Assert.Equal(expected, actual);
            Assert.DoesNotContain(
                $"{fixture.Prefix}_{formatter.GetMessageName(typeof(PackageEvent))}",
                actual);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }
}
