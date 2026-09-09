using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Configuration.Qos;

public sealed class EndpointTransportQosTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-V5-ENDPOINT-QOS", "empty-and-positive-values")]
    public void EmptyAndPositiveDeclarations_AreValidAndReportSpecificationExactly()
    {
        var empty = new EndpointTransportQos();
        var populated = new EndpointTransportQos { PrefetchCount = 32, ConcurrentDeliveryLimit = 8 };

        Assert.False(empty.IsSpecified);
        Assert.Same(empty, empty.Validate());
        Assert.True(populated.IsSpecified);
        Assert.Same(populated, populated.Validate());
        Assert.Equal(32, populated.PrefetchCount);
        Assert.Equal(8, populated.ConcurrentDeliveryLimit);
    }

    [Theory]
    [InlineData(0, null, "PrefetchCount")]
    [InlineData(-1, null, "PrefetchCount")]
    [InlineData(null, 0, "ConcurrentDeliveryLimit")]
    [InlineData(null, -1, "ConcurrentDeliveryLimit")]
    [RequirementCoverage("REQ-VSB-V5-ENDPOINT-QOS", "nonpositive-values-rejected")]
    public void Validate_RejectsEveryNonPositiveSpecifiedValue(int? prefetch, int? concurrency, string parameter)
    {
        var qos = new EndpointTransportQos
        {
            PrefetchCount = prefetch,
            ConcurrentDeliveryLimit = concurrency
        };

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(qos.Validate);
        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-ENDPOINT-QOS", "declaration-validates-all-boundaries")]
    public void Declaration_RejectsInvalidEndpointConsumerOwnerAndNestedQos()
    {
        Assert.Equal("EndpointName", Assert.Throws<ArgumentException>(() =>
            new EndpointQosDeclaration(" ", typeof(Consumer), new EndpointTransportQos(), EndpointQosOwnership.Endpoint).Validate()).ParamName);
        Assert.Equal("ConsumerType", Assert.Throws<ArgumentNullException>(() =>
            new EndpointQosDeclaration("endpoint", null!, new EndpointTransportQos(), EndpointQosOwnership.Endpoint).Validate()).ParamName);
        Assert.Equal("Qos", Assert.Throws<ArgumentNullException>(() =>
            new EndpointQosDeclaration("endpoint", typeof(Consumer), null!, EndpointQosOwnership.Endpoint).Validate()).ParamName);
        Assert.Equal("Ownership", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EndpointQosDeclaration("endpoint", typeof(Consumer), new EndpointTransportQos(), (EndpointQosOwnership)42).Validate()).ParamName);
        Assert.Equal("PrefetchCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EndpointQosDeclaration("endpoint", typeof(Consumer), new EndpointTransportQos { PrefetchCount = 0 }, EndpointQosOwnership.Endpoint)
                .Validate()).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-ENDPOINT-QOS", "exception-message-required")]
    public void ConfigurationFailure_RequiresADiagnosticMessage()
    {
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentException>(() => new EndpointQosConfigurationException(" ")).ParamName);
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => new EndpointQosConfigurationException(null!)).ParamName);
    }

    private sealed class Consumer;
}
