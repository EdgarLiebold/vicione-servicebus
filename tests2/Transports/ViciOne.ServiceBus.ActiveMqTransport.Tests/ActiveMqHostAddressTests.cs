using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests;

public sealed class ActiveMqHostAddressTests
{
    [Theory]
    [InlineData("activemq://broker:61616/", "activemq", 61616, "/", "activemq://broker:61616/")]
    [InlineData("amqp://broker:5672/production%2Fclient", "amqp", 5672, "production/client", "amqp://broker:5672/production%2Fclient")]
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
    [InlineData("activemq://user:secret@broker:61616/", "Credentials")]
    [InlineData("activemq://broker:61616/?password=secret", "typed host configurator")]
    [InlineData("activemq://broker:61616/#fragment", "typed host configurator")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "secrets-and-options-never-live-in-uri")]
    public void CredentialsAndTransportOptions_AreRejectedInHostUris(string source, string expectedReason)
    {
        ActiveMqTransportConfigurationException exception = Assert.Throws<ActiveMqTransportConfigurationException>(
            () => new ActiveMqHostAddress(new Uri(source)));

        Assert.Contains(expectedReason, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ActiveMqTransportProtocol.OpenWire, "activemq", 61616)]
    [InlineData(ActiveMqTransportProtocol.Amqp, "amqp", 5672)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "typed-protocol-construction")]
    public void ExplicitConstruction_RequiresAndProjectsTheTypedProtocol(
        ActiveMqTransportProtocol protocol,
        string expectedScheme,
        int port)
    {
        var address = new ActiveMqHostAddress(protocol, "broker", port, "/");

        Assert.Equal(expectedScheme, address.Scheme);
        Assert.Equal(new Uri($"{expectedScheme}://broker:{port}/"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "unknown-typed-protocol-fails-fast")]
    public void ExplicitConstruction_RejectsAnUnknownTypedProtocol()
    {
        var protocol = (ActiveMqTransportProtocol)999;

        var exception = Assert.Throws<ActiveMqTransportConfigurationException>(
            () => new ActiveMqHostAddress(protocol, "broker", 61616, "/"));

        Assert.Contains("not supported", exception.Message, StringComparison.Ordinal);
        Assert.Contains("999", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "uri-invalid-explicit-host-fails-at-construction")]
    public void ExplicitConstruction_RejectsUriInvalidHostsAtConstruction()
    {
        string[] invalidHosts = ["broker/path", "broker?query", "broker#fragment"];

        Assert.All(invalidHosts, host =>
        {
            var exception = Assert.Throws<ActiveMqTransportConfigurationException>(
                () => new ActiveMqHostAddress(ActiveMqTransportProtocol.OpenWire, host, 61616, "/"));

            Assert.Contains("host is invalid", exception.Message, StringComparison.Ordinal);
            Assert.Contains(host, exception.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "protocol-less-constructor-is-not-public-api")]
    public void PublicApi_ExposesNoProtocolLessHostConstructor()
    {
        Type[][] constructorShapes = typeof(ActiveMqHostAddress)
            .GetConstructors()
            .Select(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray())
            .ToArray();

        Assert.Equal(2, constructorShapes.Length);
        Assert.Contains(constructorShapes, parameters => parameters.SequenceEqual([typeof(Uri)]));
        Assert.Contains(constructorShapes, parameters => parameters.SequenceEqual(
            [typeof(ActiveMqTransportProtocol), typeof(string), typeof(int), typeof(string)]));
        Assert.Equal(typeof(int), typeof(ActiveMqHostAddress).GetField(nameof(ActiveMqHostAddress.Port))?.FieldType);
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
    [InlineData("activemq://broker/")]
    [InlineData("amqp://broker/")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "port-must-be-explicit")]
    public void HostUris_RequireAnExplicitPort(string source)
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
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-ADDRESS", "invalid-explicit-port")]
    public void ExplicitConstruction_RejectsInvalidPorts(int port)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActiveMqHostAddress("activemq", "broker", port, "/"));
    }
}
