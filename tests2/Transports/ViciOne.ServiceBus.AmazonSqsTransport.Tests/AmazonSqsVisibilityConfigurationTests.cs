using Amazon;
using ViciOne.ServiceBus.AmazonSqsTransport.Configuration;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Tests;

public sealed class AmazonSqsVisibilityConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-VISIBILITY", "duration-and-service-boundaries")]
    public void VisibilityDurations_ValidateAndClampAtAwsBoundaries()
    {
        var topology = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var bus = new AmazonSqsBusConfiguration(topology);
        bus.HostConfiguration.Settings = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1 };
        IAmazonSqsReceiveEndpointConfigurator? configurator = null;
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            bus.HostConfiguration.CreateReceiveEndpointConfiguration("orders", value => configurator = value));
        Assert.NotNull(configurator);

        configurator.WaitTimeSeconds = 20;
        configurator.RedeliverVisibilityTimeout = 43_200;
        configurator.MaxVisibilityTimeout = TimeSpan.FromHours(13);
        configurator.MaxVisibilityTimeoutRenewal = 1;
        configurator.ConcurrentDeliveryLimit = 1;

        ReceiveSettings settings = endpoint.Settings;
        Assert.Equal(20, settings.WaitTimeSeconds);
        Assert.Equal(43_200, settings.RedeliverVisibilityTimeout);
        Assert.Equal(TimeSpan.FromHours(12), settings.MaxVisibilityTimeout);
        Assert.Equal(60, settings.MaxVisibilityTimeoutRenewal);
        Assert.DoesNotContain(endpoint.Validate(), result => result.Disposition == ValidationResultDisposition.Failure);

        Assert.Throws<ArgumentOutOfRangeException>(() => configurator.WaitTimeSeconds = 21);
        Assert.Throws<ArgumentOutOfRangeException>(() => configurator.RedeliverVisibilityTimeout = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => configurator.RedeliverVisibilityTimeout = 43_201);
        Assert.Throws<ArgumentOutOfRangeException>(() => configurator.MaxVisibilityTimeout = TimeSpan.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => configurator.MaxVisibilityTimeoutRenewal = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => configurator.MaxVisibilityTimeoutRenewal = 43_201);
        Assert.Throws<ArgumentOutOfRangeException>(() => configurator.ConcurrentDeliveryLimit = 0);
    }
}
