using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusEndpointAddressTests
{
    static readonly Uri Host = new("sb://localhost/test-scope");

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\0")]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-ADDRESS", "asb-entity-name-rejects-final-lf-crlf-nul")]
    public void EntityNameValidation_RejectsUnsupportedTrailingCharacters(string suffix)
    {
        var validator = global::ViciOne.ServiceBus.AzureServiceBus.Topology.ServiceBusEntityNameValidator.Validator;
        Assert.True(validator.IsValidEntityName("orders"));
        validator.ThrowIfInvalidEntityName("orders");

        string invalid = "orders" + suffix;
        Assert.False(validator.IsValidEntityName(invalid));
        Assert.Throws<ConfigurationException>(() => validator.ThrowIfInvalidEntityName(invalid));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-ADDRESS", "custom-namespace-port-survives-host-and-entity-address-projection")]
    public void CustomPort_RemainsOnScopedHostAndEveryEntityAddressForm()
    {
        var host = new Uri("sb://localhost:5672/test-scope");
        var hostAddress = new ServiceBusHostAddress(host);

        Assert.Equal(5672, hostAddress.Port);
        Assert.Equal(host, (Uri)hostAddress);

        var named = new ServiceBusEndpointAddress(host, "input_queue");
        var relative = new ServiceBusEndpointAddress(host, new Uri("queue:input_queue"));
        var absolute = new ServiceBusEndpointAddress(host, new Uri("sb://localhost:5672/test-scope/input_queue"));
        var topic = new ServiceBusEndpointAddress(host, new Uri("topic:private-topic"));

        Assert.Equal(5672, named.Port);
        Assert.Equal(5672, relative.Port);
        Assert.Equal(5672, absolute.Port);
        Assert.Equal(5672, topic.Port);
        Assert.Equal(new Uri("sb://localhost:5672/test-scope/input_queue"), (Uri)named);
        Assert.Equal((Uri)named, (Uri)relative);
        Assert.Equal((Uri)named, (Uri)absolute);
        Assert.Equal(new Uri("sb://localhost:5672/private-topic?type=topic"), (Uri)topic);
    }

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
