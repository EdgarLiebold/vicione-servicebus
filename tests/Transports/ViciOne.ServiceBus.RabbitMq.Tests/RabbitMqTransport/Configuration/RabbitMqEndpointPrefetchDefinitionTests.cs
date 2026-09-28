using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class RabbitMqEndpointPrefetchDefinitionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-CONFIGURATION", "definition-prefetch-ushort-maximum-reaches-settings")]
    public void EndpointDefinition_ExactUShortMaximumReachesRabbitMqSettings()
    {
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topology);
        RabbitMqReceiveEndpointConfiguration? endpoint = null;

        busConfiguration.HostConfiguration.ReceiveEndpoint(
            new TemporaryEndpointDefinition("prefetch-maximum", prefetchCount: ushort.MaxValue),
            null,
            (Action<IRabbitMqReceiveEndpointConfigurator>)(configured =>
                endpoint = Assert.IsType<RabbitMqReceiveEndpointConfiguration>(configured)));

        var actual = Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint);
        Assert.Equal(ushort.MaxValue, actual.PrefetchCount);
        Assert.Equal(ushort.MaxValue, Assert.IsType<RabbitMqReceiveSettings>(actual.Settings).PrefetchCount);
        Assert.DoesNotContain(busConfiguration.HostConfiguration.Validate(), result =>
            result.Key.Contains("PrefetchCount", StringComparison.Ordinal)
            && result.Disposition == ValidationResultDisposition.Failure);
    }

    [Theory]
    [InlineData(ushort.MaxValue + 1, null, ushort.MaxValue + 1)]
    [InlineData(null, 60_000, 72_000)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-QUEUE-CONFIGURATION", "definition-prefetch-above-ushort-rejected-before-qos")]
    public void EndpointDefinition_AboveUShortMaximumFailsValidationWithoutWrappingToZero(
        int? specifiedPrefetch, int? concurrentLimit, int expectedPrefetch)
    {
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topology);
        RabbitMqReceiveEndpointConfiguration? endpoint = null;

        busConfiguration.HostConfiguration.ReceiveEndpoint(
            new TemporaryEndpointDefinition("prefetch-overflow", concurrentLimit, specifiedPrefetch),
            null,
            (Action<IRabbitMqReceiveEndpointConfigurator>)(configured =>
                endpoint = Assert.IsType<RabbitMqReceiveEndpointConfiguration>(configured)));

        var actual = Assert.IsType<RabbitMqReceiveEndpointConfiguration>(endpoint);
        Assert.Equal(expectedPrefetch, actual.PrefetchCount);
        ValidationResult failure = Assert.Single(busConfiguration.HostConfiguration.Validate(), result =>
            result.Key.Contains("PrefetchCount", StringComparison.Ordinal)
            && result.Disposition == ValidationResultDisposition.Failure);
        Assert.Contains("65535", failure.Message, StringComparison.Ordinal);

        var settings = Assert.IsType<RabbitMqReceiveSettings>(actual.Settings);
        ArgumentOutOfRangeException projection = Assert.Throws<ArgumentOutOfRangeException>(() => _ = settings.PrefetchCount);
        Assert.Equal("PrefetchCount", projection.ParamName);
    }
}
