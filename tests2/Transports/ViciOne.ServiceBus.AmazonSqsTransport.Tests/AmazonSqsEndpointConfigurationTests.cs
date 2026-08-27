using global::Amazon;
using global::Amazon.SQS;
using ViciOne.ServiceBus.AmazonSqsTransport.Configuration;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Tests;

public sealed class AmazonSqsEndpointConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "prefetch-and-concurrency-inheritance")]
    public void ConcurrencyAndPrefetch_ResolveFromEndpointAndBusSettings()
    {
        var topology = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var parent = new AmazonSqsEndpointConfiguration(topology);
        parent.Transport.Configurator.PrefetchCount = 427;

        var inherited = new QueueReceiveSettings(parent.CreateEndpointConfiguration(false), "inherited", true, false);
        Assert.Equal(427, inherited.PrefetchCount);
        Assert.Equal(427, inherited.ConcurrentMessageLimit);

        IAmazonSqsEndpointConfiguration child = parent.CreateEndpointConfiguration(false);
        child.Transport.Configurator.PrefetchCount = 351;
        child.Transport.Configurator.ConcurrentMessageLimit = 100;
        var overridden = new QueueReceiveSettings(child, "overridden", true, false);
        Assert.Equal(351, overridden.PrefetchCount);
        Assert.Equal(100, overridden.ConcurrentMessageLimit);

        parent.Transport.Configurator.ConcurrentMessageLimit = 120;
        var inheritedConcurrency = new QueueReceiveSettings(parent.CreateEndpointConfiguration(false), "parent-limit", true, false);
        Assert.Equal(427, inheritedConcurrency.PrefetchCount);
        Assert.Equal(120, inheritedConcurrency.ConcurrentMessageLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-FAULT-OWNERSHIP", "native-redrive-conflict-rejected")]
    public void NativeRedrivePolicy_IsRejectedWhileProductErrorAndSkippedQueuesOwnSettlement()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 };
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration(
                "orders",
                configurator => configurator.QueueAttributes[QueueAttributeName.RedrivePolicy] = "{}"));

        ValidationResult failure = Assert.Single(
            endpoint.Validate(),
            result => result.Disposition == ValidationResultDisposition.Failure);
        Assert.Contains("RedrivePolicy", failure.Key, StringComparison.Ordinal);
        Assert.Contains("error and skipped", failure.Message, StringComparison.Ordinal);
    }

    private static AmazonSqsBusConfiguration CreateBusConfiguration() =>
        new(new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology()));
}
