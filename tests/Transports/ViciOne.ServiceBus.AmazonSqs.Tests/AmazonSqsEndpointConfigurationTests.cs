using global::Amazon;
using global::Amazon.SQS;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

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
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
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

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "sns-envelope-requirement-is-explicit")]
    public void SnsNotificationEnvelope_IsOptionalByDefaultAndCanBeExplicitlyRequired()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration(
                "orders",
                configurator => configurator.RequireSnsNotificationEnvelope()));

        Assert.True(endpoint.Settings.RequiresSnsNotificationEnvelope);
        Assert.Equal("false", endpoint.Settings.QueueSubscriptionAttributes["RawMessageDelivery"]);
        Assert.DoesNotContain(endpoint.Validate(), result => result.Disposition == ValidationResultDisposition.Failure);

        var defaults = new QueueReceiveSettings(
            new AmazonSqsEndpointConfiguration(
                new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology())),
            "defaults",
            true,
            false);
        Assert.False(defaults.RequiresSnsNotificationEnvelope);
    }

    [Theory]
    [InlineData("false", false)]
    [InlineData("true", true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "sns-envelope-and-raw-delivery-conflicts-are-rejected")]
    public void SnsNotificationEnvelopeAndRawDelivery_RejectContradictoryConfiguration(
        string rawMessageDelivery,
        bool requireEnvelope)
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", configurator =>
            {
                if (requireEnvelope)
                    configurator.RequireSnsNotificationEnvelope();
                configurator.QueueSubscriptionAttributes["RawMessageDelivery"] = rawMessageDelivery;
            }));

        ValidationResult failure = Assert.Single(
            endpoint.Validate(),
            result => result.Disposition == ValidationResultDisposition.Failure
                && result.Key.Contains("RawMessageDelivery", StringComparison.Ordinal));
        Assert.Contains("RequireSnsNotificationEnvelope", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "case-variant-raw-delivery-cannot-bypass-envelope-validation")]
    public void SnsNotificationEnvelopeAndCaseVariantRawDelivery_RejectContradictoryConfiguration()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", configurator =>
            {
                configurator.RequireSnsNotificationEnvelope();
                configurator.QueueSubscriptionAttributes["rawmessagedelivery"] = "true";
            }));

        KeyValuePair<string, object> attribute = Assert.Single(endpoint.Settings.QueueSubscriptionAttributes);
        Assert.Equal("RawMessageDelivery", attribute.Key);
        Assert.Equal("true", attribute.Value);

        ValidationResult failure = Assert.Single(
            endpoint.Validate(),
            result => result.Disposition == ValidationResultDisposition.Failure
                && result.Key.Contains("RawMessageDelivery", StringComparison.Ordinal));
        Assert.Contains("RequireSnsNotificationEnvelope", failure.Message, StringComparison.Ordinal);
    }

    private static AmazonSqsBusConfiguration CreateBusConfiguration() =>
        new(new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology()));
}
