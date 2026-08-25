using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests;

public sealed class RabbitMqHostAddressTests
{
    [Theory]
    [InlineData("rabbitmq://broker/vhost", "rabbitmq", 5672, "rabbitmq://broker/vhost")]
    [InlineData("amqp://broker/vhost", "amqp", 5672, "amqp://broker/vhost")]
    [InlineData("rabbitmqs://broker/vhost", "rabbitmqs", 5671, "rabbitmqs://broker/vhost")]
    [InlineData("amqps://broker/vhost", "amqps", 5671, "amqps://broker/vhost")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-ADDRESS", "supported-schemes-and-default-ports")]
    public void SupportedSchemes_UseTheirDefaultPortAndRoundTrip(
        string source,
        string expectedScheme,
        int expectedPort,
        string expectedAddress)
    {
        var address = new RabbitMqHostAddress(new Uri(source));

        Assert.Equal(expectedScheme, address.Scheme);
        Assert.Equal("broker", address.Host);
        Assert.Equal(expectedPort, address.Port);
        Assert.Equal("vhost", address.VirtualHost);
        Assert.Equal(new Uri(expectedAddress), (Uri)address);
    }

    [Theory]
    [InlineData("rabbitmq://broker:0/vhost", 5672, "rabbitmq://broker/vhost")]
    [InlineData("rabbitmq://broker:5672/vhost", 5672, "rabbitmq://broker/vhost")]
    [InlineData("rabbitmqs://broker:0/vhost", 5671, "rabbitmqs://broker/vhost")]
    [InlineData("rabbitmqs://broker:5671/vhost", 5671, "rabbitmqs://broker/vhost")]
    [InlineData("rabbitmq://broker:25672/vhost", 25672, "rabbitmq://broker:25672/vhost")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-ADDRESS", "canonical-port-rendering")]
    public void Ports_AreNormalizedAndRenderedCanonically(string source, int expectedPort, string expectedAddress)
    {
        var address = new RabbitMqHostAddress(new Uri(source));

        Assert.Equal(expectedPort, address.Port);
        Assert.Equal(new Uri(expectedAddress), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-ADDRESS", "encoded-virtual-host-roundtrip")]
    public void EncodedVirtualHost_IsDecodedInMemoryAndEncodedInTheUri()
    {
        var address = new RabbitMqHostAddress(new Uri("rabbitmq://broker/production%2Fclient"));

        Assert.Equal("production/client", address.VirtualHost);
        Assert.Equal(new Uri("rabbitmq://broker/production%2Fclient"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-OPTIONS", "numeric-options")]
    public void NumericOptions_AreParsedWithTheirExactTypes()
    {
        var address = new RabbitMqHostAddress(
            new Uri("rabbitmq://broker/vhost?heartbeat=30&prefetch=32&ttl=30000"));

        Assert.Equal((ushort)30, address.Heartbeat);
        Assert.Equal((ushort)32, address.Prefetch);
        Assert.Equal(30000, address.TimeToLive);
        Assert.Equal(
            new Uri("rabbitmq://broker/vhost?heartbeat=30&prefetch=32&ttl=30000"),
            (Uri)address);
    }

    [Theory]
    [InlineData("rabbitmq://broker/vhost?heartbeat=-1")]
    [InlineData("rabbitmq://broker/vhost?heartbeat=65536")]
    [InlineData("rabbitmq://broker/vhost?prefetch=not-a-number")]
    [InlineData("rabbitmq://broker/vhost?ttl=-1")]
    [InlineData("rabbitmq://broker/vhost?ttl=")]
    [InlineData("rabbitmq://broker/vhost?prefetch=1&prefetch=2")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-OPTIONS", "invalid-or-ambiguous-values-fail-fast")]
    public void InvalidOrDuplicateNumericOptions_AreRejected(string source)
    {
        Assert.Throws<RabbitMqAddressException>(() => new RabbitMqHostAddress(new Uri(source)));
    }

    [Theory]
    [InlineData("http://broker/vhost")]
    [InlineData("loopback://broker/vhost")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-ADDRESS", "unsupported-scheme")]
    public void UnsupportedScheme_IsRejected(string source)
    {
        Assert.Throws<RabbitMqAddressException>(() => new RabbitMqHostAddress(new Uri(source)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-OPTIONS", "unknown-option-fails-fast")]
    public void UnknownHostOptions_AreRejected()
    {
        Assert.Throws<RabbitMqAddressException>(() =>
            new RabbitMqHostAddress(new Uri("rabbitmq://broker/vhost?not-an-option=true")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-ADDRESS", "explicit-secure-custom-port")]
    public void ExplicitConstruction_PreservesTlsOnACustomPort()
    {
        var address = new RabbitMqHostAddress("broker", 25671, "production", useTls: true);

        Assert.Equal(RabbitMqHostAddress.RabbitMqSecureScheme, address.Scheme);
        Assert.Equal(25671, address.Port);
        Assert.Equal(new Uri("rabbitmqs://broker:25671/production"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-ADDRESS", "explicit-default-port-canonicalization")]
    public void ExplicitConstruction_OmitsTheSchemeDefaultPort()
    {
        var address = new RabbitMqHostAddress("broker", 5672, "production");

        Assert.Equal(5672, address.Port);
        Assert.Equal(new Uri("rabbitmq://broker/production"), (Uri)address);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-ADDRESS", "security-is-explicit-not-port-inferred")]
    public void ExplicitConstruction_DoesNotInferTlsFromThePortNumber()
    {
        var address = new RabbitMqHostAddress("broker", 5671, "production");

        Assert.Equal(RabbitMqHostAddress.RabbitMqScheme, address.Scheme);
        Assert.Equal(new Uri("rabbitmq://broker:5671/production"), (Uri)address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-ADDRESS", "invalid-explicit-input")]
    public void ExplicitConstruction_RejectsInvalidHosts(string? host)
    {
        Assert.Throws<ArgumentException>(() => new RabbitMqHostAddress(host, 5672, "/"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-ADDRESS", "invalid-explicit-port")]
    public void ExplicitConstruction_RejectsInvalidPorts(int port)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RabbitMqHostAddress("broker", port, "/"));
    }
}
