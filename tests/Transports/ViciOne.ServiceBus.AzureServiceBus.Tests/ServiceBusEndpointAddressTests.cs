using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusEndpointAddressTests
{
    static readonly Uri Host = new("sb://localhost/test-scope");

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-ADDRESS", "topic-short-address-canonical-roundtrip")]
    public void TopicShortAddress_RoundTripsWithItsProviderType()
    {
        var address = new ServiceBusEndpointAddress(Host, new Uri("topic:private-topic"));
        Uri uri = address;

        Assert.Equal(ServiceBusEndpointAddress.AddressType.Topic, address.Type);
        Assert.Equal("private-topic", address.Name);
        Assert.Equal("/private-topic", uri.AbsolutePath);
        Assert.Equal("type=topic", uri.Query.TrimStart('?'));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-ADDRESS", "canonical-address-is-idempotent")]
    public void CanonicalAddress_NormalizesIdempotently()
    {
        var first = new ServiceBusEndpointAddress(Host, new Uri("topic:private-topic"));
        var second = new ServiceBusEndpointAddress(Host, (Uri)first);

        Assert.Equal(first.Type, second.Type);
        Assert.Equal(first.Name, second.Name);
        Assert.Equal(first.Scope, second.Scope);
        Assert.Equal((Uri)first, (Uri)second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-ADDRESS", "queue-name-scope-and-path")]
    public void QueueName_ProjectsTheExactScopePathAndUri()
    {
        var address = new ServiceBusEndpointAddress(Host, "input_queue");

        Assert.Equal("input_queue", address.Name);
        Assert.Equal("test-scope", address.Scope);
        Assert.Equal("test-scope/input_queue", address.Path);
        Assert.Equal(new Uri("sb://localhost/test-scope/input_queue"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-ADDRESS", "unsupported-scheme-fails-fast")]
    public void UnsupportedScheme_IsRejectedAtTheDomainBoundary()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new ServiceBusEndpointAddress(Host, new Uri("https://localhost/entity")));

        Assert.Contains("not supported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
