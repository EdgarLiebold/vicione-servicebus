using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsEndpointAddressTests
{
    private static readonly Uri HostAddress = new("amazonsqs://eu-central-1/production");

    [Theory]
    [InlineData("amazonsqs://eu-central-1", "/", "eu-central-1", "amazonsqs://eu-central-1/")]
    [InlineData("amazonsqs://eu-central-1/production", "production", "eu-central-1", "amazonsqs://eu-central-1/production")]
    [InlineData("amazonsqs://eu-central-1/production%2Fblue", "production/blue", "eu-central-1", "amazonsqs://eu-central-1/production%2Fblue")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ADDRESS", "host-and-endpoint-region-scope-path")]
    public void HostAndEndpointAddresses_PreserveRegionScopeAndPath(
        string source,
        string expectedScope,
        string expectedHost,
        string expectedAddress)
    {
        var host = new AmazonSqsHostAddress(new Uri(source));

        Assert.Equal(expectedScope, host.Scope);
        Assert.Equal(expectedHost, host.Host);
        Assert.Equal(new Uri(expectedAddress), (Uri)host);

        var endpoint = new AmazonSqsEndpointAddress(
            (Uri)host,
            new Uri($"amazonsqs://{expectedHost}/{(expectedScope == "/" ? string.Empty : Uri.EscapeDataString(expectedScope) + "/")}orders"));
        Assert.Equal(expectedScope, endpoint.Scope);
        Assert.Equal("orders", endpoint.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ADDRESS", "queue-address-classification-roundtrip")]
    public void QueueAddress_IsClassifiedAndRoundTrips()
    {
        var address = new AmazonSqsEndpointAddress(HostAddress, new Uri("queue:orders"));

        Assert.Equal(AmazonSqsEndpointAddress.AddressType.Queue, address.Type);
        Assert.Equal("orders", address.Name);
        Assert.True(address.Durable);
        Assert.False(address.AutoDelete);
        Assert.Equal(new Uri("amazonsqs://eu-central-1/production/orders"), (Uri)address);
    }

    [Theory]
    [InlineData("topic:orders", true, false)]
    [InlineData("topic:orders?temporary=true", false, true)]
    [InlineData("topic:orders?durable=false", false, false)]
    [InlineData("topic:orders?autodelete=true", true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SNS-ADDRESS", "topic-lifetime-flags")]
    public void TopicAddress_PreservesLifetimeFlags(string source, bool expectedDurable, bool expectedAutoDelete)
    {
        var address = new AmazonSqsEndpointAddress(HostAddress, new Uri(source));

        Assert.Equal(AmazonSqsEndpointAddress.AddressType.Topic, address.Type);
        Assert.Equal("production_orders", address.Name);
        Assert.Equal(expectedDurable, address.Durable);
        Assert.Equal(expectedAutoDelete, address.AutoDelete);

        var roundTripped = new AmazonSqsEndpointAddress(HostAddress, (Uri)address);
        Assert.Equal(address.Name, roundTripped.Name);
        Assert.Equal(address.Type, roundTripped.Type);
        Assert.Equal(address.Durable, roundTripped.Durable);
        Assert.Equal(address.AutoDelete, roundTripped.AutoDelete);
    }

    [Theory]
    [InlineData("queue:orders?unknown=true")]
    [InlineData("queue:orders?durable=not-a-boolean")]
    [InlineData("queue:orders?durable=true&durable=false")]
    [InlineData("topic:orders?type=invalid")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-ADDRESS", "invalid-or-ambiguous-options-fail-fast")]
    public void InvalidOrAmbiguousEndpointOptions_AreRejected(string source)
    {
        Assert.Throws<AmazonSqsTransportConfigurationException>(
            () => new AmazonSqsEndpointAddress(HostAddress, new Uri(source)));
    }

    [Theory]
    [InlineData("orders?temporary=true")]
    [InlineData("orders#fragment")]
    [InlineData("orders/path")]
    [RequirementCoverage("REQ-VSB-AWS-SNS-ADDRESS", "caller-topic-names-are-data")]
    public void TopologyDestinationNames_AreValidatedAsData(string name)
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        busConfiguration.HostConfiguration.Settings = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
        }.Freeze();

        Assert.Throws<AmazonSqsTransportConfigurationException>(
            () => busConfiguration.HostConfiguration.Topology.GetDestinationAddress(name));
    }
}
