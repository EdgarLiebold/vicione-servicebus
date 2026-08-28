using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests;

public sealed class ActiveMqHostAddressTests
{
    [Theory]
    [InlineData("activemq://broker/", "activemq", 61616, "/", "activemq://broker/")]
    [InlineData("amqp://broker/production%2Fclient", "amqp", 61616, "production/client", "amqp://broker/production%2Fclient")]
    [InlineData("activemq://broker:26161/production", "activemq", 26161, "production", "activemq://broker:26161/production")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "supported-schemes-ports-and-scope")]
    public void SupportedAddresses_RoundTripCanonically(
        string source,
        string expectedScheme,
        int expectedPort,
        string expectedVirtualHost,
        string expectedAddress)
    {
        var address = new ActiveMqHostAddress(new Uri(source));

        Assert.Equal(expectedScheme, address.Scheme);
        Assert.Equal("broker", address.Host);
        Assert.Equal(expectedPort, address.Port);
        Assert.Equal(expectedVirtualHost, address.VirtualHost);
        Assert.Equal(new Uri(expectedAddress), (Uri)address);
    }

    [Theory]
    [InlineData("activemq://user:secret@broker/")]
    [InlineData("activemq://broker/?password=secret")]
    [InlineData("activemq://broker/#fragment")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "secrets-and-options-never-live-in-uri")]
    public void CredentialsAndTransportOptions_AreRejectedInHostUris(string source)
    {
        Assert.Throws<ActiveMqTransportConfigurationException>(() => new ActiveMqHostAddress(new Uri(source)));
    }

    [Theory]
    [InlineData("http://broker/")]
    [InlineData("tcp://broker/")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "unsupported-scheme-fails-fast")]
    public void UnsupportedSchemes_AreRejected(string source)
    {
        Assert.Throws<ActiveMqTransportConfigurationException>(() => new ActiveMqHostAddress(new Uri(source)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "invalid-explicit-host")]
    public void ExplicitConstruction_RejectsInvalidHosts(string? host)
    {
        Assert.Throws<ArgumentException>(() => new ActiveMqHostAddress("activemq", host!, 61616, "/"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "invalid-explicit-port")]
    public void ExplicitConstruction_RejectsInvalidPorts(int port)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActiveMqHostAddress("activemq", "broker", port, "/"));
    }
}
