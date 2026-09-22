using global::Amazon;
using global::Amazon.SQS;
using System.Linq;
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

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "mutable-receive-settings-cannot-bypass-operational-bounds")]
    public void MutableReceiveSettings_RejectOutOfRangeOperationalLimitsBeforeStartup()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", configurator =>
            {
                configurator.PrefetchCount = 0;
                configurator.ConcurrentMessageLimit = 0;
            }));
        var settings = Assert.IsType<QueueReceiveSettings>(endpoint.Settings);
        settings.ConcurrentDeliveryLimit = 0;
        settings.WaitTimeSeconds = 21;
        settings.RedeliverVisibilityTimeout = 43_201;
        settings.MaxVisibilityTimeout = TimeSpan.FromSeconds(29);
        settings.MaxVisibilityTimeoutRenewal = 43_201;

        ValidationResult[] failures = endpoint.Validate()
            .Where(result => result.Disposition == ValidationResultDisposition.Failure)
            .ToArray();

        Assert.Contains(failures, result => result.Key.Contains("PrefetchCount", StringComparison.Ordinal)
            && result.Message.Contains("must be >= 1", StringComparison.Ordinal));
        Assert.Contains(failures, result => result.Key.Contains("ConcurrentMessageLimit", StringComparison.Ordinal)
            && result.Message.Contains("must be >= 1", StringComparison.Ordinal));
        Assert.Contains(failures, result => result.Key.Contains("ConcurrentDeliveryLimit", StringComparison.Ordinal)
            && result.Message.Contains("must be >= 1", StringComparison.Ordinal));
        Assert.Contains(failures, result => result.Key.Contains("WaitTimeSeconds", StringComparison.Ordinal)
            && result.Message.Contains("between 0 and 20", StringComparison.Ordinal));
        Assert.Contains(failures, result => result.Key.Contains("RedeliverVisibilityTimeout", StringComparison.Ordinal)
            && result.Message.Contains("between 0 and 43200", StringComparison.Ordinal));
        Assert.Contains(failures, result => result.Key.Contains("MaxVisibilityTimeout", StringComparison.Ordinal)
            && result.Message.Contains("greater than or equal to VisibilityTimeout", StringComparison.Ordinal));
        Assert.Contains(failures, result => result.Key.Contains("MaxVisibilityTimeoutRenewal", StringComparison.Ordinal)
            && result.Message.Contains("<= 43200", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "mutable-negative-receive-settings-are-rejected")]
    public void MutableReceiveSettings_RejectNegativePollingAndVisibilityValues()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", _ => { }));
        var settings = Assert.IsType<QueueReceiveSettings>(endpoint.Settings);
        settings.WaitTimeSeconds = -1;
        settings.RedeliverVisibilityTimeout = -1;
        settings.MaxVisibilityTimeoutRenewal = -1;

        ValidationResult[] failures = endpoint.Validate()
            .Where(result => result.Disposition == ValidationResultDisposition.Failure)
            .ToArray();
        Assert.Contains(failures, result => result.Key.Contains("WaitTimeSeconds", StringComparison.Ordinal)
            && result.Message.Contains("between 0 and 20", StringComparison.Ordinal));
        Assert.Contains(failures, result => result.Key.Contains("RedeliverVisibilityTimeout", StringComparison.Ordinal)
            && result.Message.Contains("between 0 and 43200", StringComparison.Ordinal));
        Assert.Contains(failures, result => result.Key.Contains("MaxVisibilityTimeoutRenewal", StringComparison.Ordinal)
            && result.Message.Contains(">= 0", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "purge-warning-tracks-the-effective-setting")]
    public void PurgeOnStartup_WarnsOnlyWhileDestructiveStartupIsEnabled()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", _ => { }));
        var settings = Assert.IsType<QueueReceiveSettings>(endpoint.Settings);

        Assert.DoesNotContain(endpoint.Validate(), result => result.Disposition == ValidationResultDisposition.Warning
            && result.Message.Contains("purged", StringComparison.Ordinal));
        settings.PurgeOnStartup = true;
        ValidationResult warning = Assert.Single(endpoint.Validate(), result => result.Disposition == ValidationResultDisposition.Warning
            && result.Message.Contains("purged", StringComparison.Ordinal));
        Assert.Contains("orders", warning.Key, StringComparison.Ordinal);
        settings.PurgeOnStartup = false;
        Assert.DoesNotContain(endpoint.Validate(), result => result.Disposition == ValidationResultDisposition.Warning
            && result.Message.Contains("purged", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "required-sns-envelope-rejects-removed-raw-delivery-setting")]
    public void RequiredSnsEnvelope_RejectsRemovalOfItsRawDeliverySetting()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", configurator =>
                configurator.RequireSnsNotificationEnvelope()));

        Assert.True(endpoint.Settings.RequiresSnsNotificationEnvelope);
        Assert.Equal("false", endpoint.Settings.QueueSubscriptionAttributes["RawMessageDelivery"]);
        Assert.True(endpoint.Settings.QueueSubscriptionAttributes.Remove("RawMessageDelivery"));

        ValidationResult failure = Assert.Single(endpoint.Validate(), result => result.Disposition == ValidationResultDisposition.Failure
            && result.Key.Contains("RawMessageDelivery", StringComparison.Ordinal));
        Assert.Contains("must be 'false'", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(43_201)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "mutable-visibility-timeout-stays-within-aws-bounds")]
    public void MutableVisibilityTimeout_RejectsBothOutOfRangeValuesWithItsOwnKey(int visibilityTimeout)
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", _ => { }));
        var settings = Assert.IsType<QueueReceiveSettings>(endpoint.Settings);
        settings.VisibilityTimeout = visibilityTimeout;

        ValidationResult failure = Assert.Single(endpoint.Validate(), result =>
            result.Disposition == ValidationResultDisposition.Failure && result.Key == "VisibilityTimeout");
        Assert.Contains("between 0 and 43200", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "mutable-maximum-visibility-cannot-exceed-aws-limit")]
    public void MutableMaximumVisibility_RejectsDurationAboveTwelveHours()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", _ => { }));
        var settings = Assert.IsType<QueueReceiveSettings>(endpoint.Settings);
        settings.MaxVisibilityTimeout = TimeSpan.FromHours(13);

        ValidationResult failure = Assert.Single(endpoint.Validate(), result =>
            result.Disposition == ValidationResultDisposition.Failure && result.Key == "MaxVisibilityTimeout");
        Assert.Contains("12 hours", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "mutable-maximum-visibility-must-be-positive")]
    public void MutableMaximumVisibility_RejectsZeroDuration()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", _ => { }));
        var settings = Assert.IsType<QueueReceiveSettings>(endpoint.Settings);
        settings.VisibilityTimeout = 0;
        settings.MaxVisibilityTimeout = TimeSpan.Zero;

        ValidationResult failure = Assert.Single(endpoint.Validate(), result =>
            result.Disposition == ValidationResultDisposition.Failure && result.Key == "MaxVisibilityTimeout");
        Assert.Contains("positive", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(59)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "mutable-visibility-renewal-enforces-minimum")]
    public void MutableVisibilityRenewal_RejectsValuesBelowEffectiveMinimum(int renewalSeconds)
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", _ => { }));
        var settings = Assert.IsType<QueueReceiveSettings>(endpoint.Settings);
        settings.MaxVisibilityTimeoutRenewal = renewalSeconds;

        ValidationResult failure = Assert.Single(endpoint.Validate(), result =>
            result.Disposition == ValidationResultDisposition.Failure && result.Key == "MaxVisibilityTimeoutRenewal");
        Assert.Contains(">= 60", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "validation-order-and-deferred-settings")]
    public void Validation_PreservesLocalDiagnosticOrderAndReadsSettingsWhenEnumerated()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", configurator =>
            {
                configurator.PrefetchCount = 0;
                configurator.ConcurrentMessageLimit = 0;
            }));
        var settings = Assert.IsType<QueueReceiveSettings>(endpoint.Settings);
        settings.ConcurrentDeliveryLimit = 0;
        settings.WaitTimeSeconds = 21;
        settings.RedeliverVisibilityTimeout = 43_201;
        settings.MaxVisibilityTimeout = TimeSpan.FromSeconds(29);
        settings.MaxVisibilityTimeoutRenewal = 43_201;
        settings.QueueAttributes[QueueAttributeName.RedrivePolicy] = "{}";
        settings.QueueSubscriptionAttributes["RawMessageDelivery"] = "invalid";

        IEnumerable<ValidationResult> validation = endpoint.Validate();
        settings.PurgeOnStartup = true;
        (string Key, ValidationResultDisposition Disposition)[] expected =
        [
            ("PrefetchCount", ValidationResultDisposition.Failure),
            ("ConcurrentMessageLimit", ValidationResultDisposition.Failure),
            ("ConcurrentDeliveryLimit", ValidationResultDisposition.Failure),
            ("WaitTimeSeconds", ValidationResultDisposition.Failure),
            ("RedeliverVisibilityTimeout", ValidationResultDisposition.Failure),
            ("orders", ValidationResultDisposition.Warning),
            ("MaxVisibilityTimeout", ValidationResultDisposition.Failure),
            ("MaxVisibilityTimeoutRenewal", ValidationResultDisposition.Failure),
            ("RedrivePolicy", ValidationResultDisposition.Failure),
            ("RawMessageDelivery", ValidationResultDisposition.Failure)
        ];

        Assert.Equal(expected, validation.Take(expected.Length).Select(result => (result.Key, result.Disposition)));

        settings.PurgeOnStartup = false;
        Assert.DoesNotContain(validation, result => result.Disposition == ValidationResultDisposition.Warning
            && result.Key == "orders");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ENDPOINT-CONFIGURATION", "raw-delivery-attribute-must-be-boolean-text")]
    public void RawMessageDelivery_RejectsMalformedTextAndNonStringAttributes()
    {
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 }.Freeze();
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", _ => { }));

        foreach (object invalidValue in new object[] { "sometimes", true, 1 })
        {
            endpoint.Settings.QueueSubscriptionAttributes["RawMessageDelivery"] = invalidValue;
            ValidationResult failure = Assert.Single(endpoint.Validate(), result =>
                result.Disposition == ValidationResultDisposition.Failure && result.Key == "RawMessageDelivery");
            Assert.Contains("string 'true' or 'false'", failure.Message, StringComparison.Ordinal);
        }
    }

    private static AmazonSqsBusConfiguration CreateBusConfiguration() =>
        new(new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology()));
}
