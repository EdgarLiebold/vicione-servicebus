using System.Collections.Frozen;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Qos;

public sealed class EndpointQosTopologyValidatorTests
{
    private readonly EndpointQosTopologyValidator _validator = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-ENDPOINT-QOS-OWNERSHIP", "matching-endpoint-declarations-converge")]
    public void MatchingEndpointDeclarations_ProduceOneFrozenCanonicalValue()
    {
        var first = new EndpointTransportQos { PrefetchCount = 16, ConcurrentDeliveryLimit = 4 };
        var equal = new EndpointTransportQos { PrefetchCount = 16, ConcurrentDeliveryLimit = 4 };
        EndpointQosDeclaration[] declarations =
        [
            new("shared", typeof(FirstConsumer), first, EndpointQosOwnership.Endpoint),
            new("shared", typeof(SecondConsumer), equal, EndpointQosOwnership.Endpoint),
            new("shared", typeof(FirstConsumer), new EndpointTransportQos(), EndpointQosOwnership.ConsumerDefinition)
        ];

        FrozenDictionary<string, EndpointTransportQos> result = _validator.Validate(declarations);

        Assert.Single(result);
        Assert.Same(first, result["shared"]);
        Assert.Equal(StringComparer.Ordinal, result.Comparer);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-ENDPOINT-QOS-OWNERSHIP", "consumer-owned-value-requires-dedicated-endpoint")]
    public void ConsumerOwnedQos_IsAcceptedOnlyWhileTheEndpointIsDedicated()
    {
        EndpointTransportQos dedicatedQos = new() { PrefetchCount = 1 };
        FrozenDictionary<string, EndpointTransportQos> dedicated = _validator.Validate(
        [
            new("dedicated", typeof(FirstConsumer), dedicatedQos, EndpointQosOwnership.ConsumerDefinition)
        ]);

        Assert.Same(dedicatedQos, dedicated["dedicated"]);

        EndpointQosDeclaration[] sharedDeclarations =
        [
            new("shared", typeof(FirstConsumer), new EndpointTransportQos { ConcurrentDeliveryLimit = 2 }, EndpointQosOwnership.ConsumerDefinition),
            new("shared", typeof(SecondConsumer), new EndpointTransportQos { PrefetchCount = 8 }, EndpointQosOwnership.Endpoint)
        ];

        EndpointQosConfigurationException exception = Assert.Throws<EndpointQosConfigurationException>(() =>
            _validator.Validate(sharedDeclarations));

        Assert.Contains("shared", exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(FirstConsumer).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Contains("endpoint boundary", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-ENDPOINT-QOS-OWNERSHIP", "conflicts-aggregate-deterministically")]
    public void ConflictingEndpointValues_AggregateWithConsumerOwnershipFailures()
    {
        EndpointQosDeclaration[] declarations =
        [
            new("shared", typeof(FirstConsumer), new EndpointTransportQos { PrefetchCount = 4 }, EndpointQosOwnership.Endpoint),
            new("shared", typeof(SecondConsumer), new EndpointTransportQos { PrefetchCount = 8 }, EndpointQosOwnership.Endpoint),
            new("shared", typeof(ThirdConsumer), new EndpointTransportQos { PrefetchCount = 4 }, EndpointQosOwnership.ConsumerDefinition)
        ];

        EndpointQosConfigurationException exception = Assert.Throws<EndpointQosConfigurationException>(() =>
            _validator.Validate(declarations));

        string[] lines = exception.Message.Split(Environment.NewLine, StringSplitOptions.None);
        Assert.Equal(2, lines.Length);
        Assert.Contains("conflicting", lines[0], StringComparison.Ordinal);
        Assert.Contains(typeof(ThirdConsumer).FullName!, lines[1], StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-ENDPOINT-QOS-OWNERSHIP", "empty-and-case-sensitive-endpoints")]
    public void EmptyDeclarationsAreIgnoredAndEndpointIdentityIsOrdinal()
    {
        EndpointQosDeclaration[] declarations =
        [
            new("orders", typeof(FirstConsumer), new EndpointTransportQos(), EndpointQosOwnership.Endpoint),
            new("ORDERS", typeof(SecondConsumer), new EndpointTransportQos { PrefetchCount = 9 }, EndpointQosOwnership.Endpoint)
        ];

        FrozenDictionary<string, EndpointTransportQos> result = _validator.Validate(declarations);

        Assert.Single(result);
        Assert.False(result.ContainsKey("orders"));
        Assert.Equal(9, result["ORDERS"].PrefetchCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-ENDPOINT-QOS-OWNERSHIP", "null-inputs-fail-before-grouping")]
    public void NullCollectionAndEntries_FailAtTheValidationBoundary()
    {
        Assert.Equal("declarations", Assert.Throws<ArgumentNullException>(() =>
            _validator.Validate(null!)).ParamName);
        Assert.Equal("declaration", Assert.Throws<ArgumentNullException>(() =>
            _validator.Validate([null!])).ParamName);
    }

    private sealed class FirstConsumer;
    private sealed class SecondConsumer;
    private sealed class ThirdConsumer;
}
