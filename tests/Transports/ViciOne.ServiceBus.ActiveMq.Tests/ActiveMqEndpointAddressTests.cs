using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests;

public sealed class ActiveMqEndpointAddressTests
{
    private static readonly Uri HostAddress = new("activemq://broker:61616/production%2Fclient");

    [Theory]
    [InlineData("activemq://remote:61616", "activemq://remote:61616/orders", "/", "orders", "activemq://remote:61616/orders")]
    [InlineData("activemq://remote:61616/production%2Fclient", "activemq://remote:61616/production/client/orders", "production/client", "orders", "activemq://remote:61616/production%2Fclient/orders")]
    [InlineData("amqp://remote:5672/production%2Fclient", "amqp://remote:5672/production%2Fclient/orders", "production/client", "orders", "amqp://remote:5672/production%2Fclient/orders")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-ENDPOINT-ADDRESS", "full-address-canonical-roundtrip")]
    public void FullAddresses_ParseAndRenderCanonically(
        string host,
        string source,
        string expectedVirtualHost,
        string expectedName,
        string expectedAddress)
    {
        var address = new ActiveMqEndpointAddress(new Uri(host), new Uri(source));

        Assert.Equal(expectedVirtualHost, address.VirtualHost);
        Assert.Equal(expectedName, address.Name);
        Assert.Equal(new Uri(expectedAddress), (Uri)address);
    }

    [Theory]
    [InlineData("queue:orders%3Apriority", "orders:priority", ActiveMqEndpointAddress.AddressType.Queue, "activemq://broker:61616/production%2Fclient/orders%3Apriority")]
    [InlineData("topic:events.test", "events.test", ActiveMqEndpointAddress.AddressType.Topic, "activemq://broker:61616/production%2Fclient/events.test?type=topic")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-ENDPOINT-ADDRESS", "short-address-symmetric-resolution")]
    public void ShortAddresses_DecodeNamesAndResolveAgainstTheHost(
        string source,
        string expectedName,
        ActiveMqEndpointAddress.AddressType expectedType,
        string expectedAddress)
    {
        var address = new ActiveMqEndpointAddress(HostAddress, new Uri(source));

        Assert.Equal(expectedName, address.Name);
        Assert.Equal(expectedType, address.Type);
        Assert.Equal(new Uri(expectedAddress), (Uri)address);
    }

    [Theory]
    [InlineData("activemq://broker:61616", "activemq://remote:61616/orders")]
    [InlineData("activemq://broker:61616", "amqp://broker:61616/orders")]
    [InlineData("activemq://broker:61616", "activemq://broker:61617/orders")]
    [InlineData("activemq://broker:61616/alpha", "activemq://broker:61616/beta/orders")]
    [InlineData("activemq://broker:61616", "activemq://user:secret@broker:61616/orders")]
    [InlineData("activemq://broker:61616", "activemq://broker:61616/orders#fragment")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-ENDPOINT-ADDRESS", "full-address-host-must-match-configuration")]
    public void FullAddresses_CannotRedirectTheConfiguredConnection(string host, string source)
    {
        Assert.Throws<ActiveMqTransportConfigurationException>(
            () => new ActiveMqEndpointAddress(new Uri(host), new Uri(source)));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\0")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-ENDPOINT-ADDRESS", "active-entity-name-rejects-final-lf-crlf-nul")]
    public void EntityNameValidation_RejectsUnsupportedTrailingCharacters(string suffix)
    {
        var validator = global::ViciOne.ServiceBus.ActiveMq.Topology.ActiveMqEntityNameValidator.Validator;
        Assert.True(validator.IsValidEntityName("orders"));
        validator.ThrowIfInvalidEntityName("orders");

        string invalid = "orders" + suffix;
        Assert.False(validator.IsValidEntityName(invalid));
        Assert.Throws<ActiveMqTransportConfigurationException>(() => validator.ThrowIfInvalidEntityName(invalid));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-ENDPOINT-OPTIONS", "complete-roundtrip")]
    public void EndpointOptions_RoundTripWithoutLosingFlags()
    {
        var first = new ActiveMqEndpointAddress(
            HostAddress,
            new Uri("activemq://broker:61616/production%2Fclient/orders?durable=false&autodelete=true&type=topic"));
        var second = new ActiveMqEndpointAddress(HostAddress, (Uri)first);

        Assert.False(second.Durable);
        Assert.True(second.AutoDelete);
        Assert.Equal(ActiveMqEndpointAddress.AddressType.Topic, second.Type);
    }

    [Theory]
    [InlineData("queue:orders?temporary=maybe")]
    [InlineData("queue:orders?unknown=true")]
    [InlineData("queue:orders?durable=true&durable=false")]
    [InlineData("queue:orders?temporary=true&durable=false")]
    [InlineData("queue:orders?durable=false&temporary=true")]
    [InlineData("queue:orders?temporary=true&autodelete=true")]
    [InlineData("queue:orders?type=topic")]
    [InlineData("topic:orders?type=queue")]
    [InlineData("topic:orders?type=invalid")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-ENDPOINT-OPTIONS", "invalid-or-ambiguous-input-fails-fast")]
    public void InvalidOrAmbiguousEndpointInputs_AreRejected(string source)
    {
        Assert.Throws<ActiveMqTransportConfigurationException>(
            () => new ActiveMqEndpointAddress(HostAddress, new Uri(source)));
    }

    [Theory]
    [InlineData("orders?temporary=true")]
    [InlineData("orders/path")]
    [InlineData("orders name")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-ENDPOINT-ADDRESS", "direct-name-is-data-not-uri-control")]
    public void DirectConstruction_RejectsNamesThatCouldBecomeUriControl(string name)
    {
        Assert.Throws<ActiveMqTransportConfigurationException>(
            () => new ActiveMqEndpointAddress(HostAddress, name));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-ENDPOINT-ADDRESS", "topology-name-is-data-not-uri-control")]
    public void BusTopology_RejectsANameThatAttemptsToInjectTemporarySemantics()
    {
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var bus = new ActiveMqBusConfiguration(topology);
        bus.HostConfiguration.Settings = new OpenWireHostSettings(new Uri("activemq://broker:61616"));

        Assert.Throws<ActiveMqTransportConfigurationException>(
            () => bus.HostConfiguration.Topology.GetDestinationAddress("orders?temporary=true"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-TOPOLOGY", "runtime-message-type-preserves-lifecycle-semantics")]
    public void BusTopology_ResolvesRuntimeMessageTypesAndAppliesLifecycleOverrides()
    {
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var bus = new ActiveMqBusConfiguration(topology);
        bus.HostConfiguration.Settings = new OpenWireHostSettings(new Uri("activemq://broker:61616"));

        Uri durable = bus.HostConfiguration.Topology.GetDestinationAddress(
            typeof(DurableMessage),
            configurator => configurator.AutoDelete = true);
        Uri temporary = bus.HostConfiguration.Topology.GetDestinationAddress(typeof(TemporaryMessage));
        var durableAddress = new ActiveMqEndpointAddress(bus.HostConfiguration.HostAddress, durable);
        var temporaryAddress = new ActiveMqEndpointAddress(bus.HostConfiguration.HostAddress, temporary);

        Assert.True(durableAddress.Durable);
        Assert.True(durableAddress.AutoDelete);
        Assert.False(temporaryAddress.Durable);
        Assert.True(temporaryAddress.AutoDelete);
        Assert.Equal(ActiveMqEndpointAddress.AddressType.Topic, durableAddress.Type);
        Assert.Equal(ActiveMqEndpointAddress.AddressType.Topic, temporaryAddress.Type);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-ENDPOINT-ADDRESS", "temporary-generated-name-is-broker-safe")]
    public void TemporaryEntityName_UsesBrokerSafeCanonicalForm()
    {
        var address = new ActiveMqEndpointAddress(HostAddress, new Uri("queue:*?temporary=true"));

        Assert.Matches("^[A-Za-z0-9_-]+$", address.Name);
        Assert.False(address.Durable);
        Assert.True(address.AutoDelete);
    }

    public sealed record DurableMessage;

    private sealed record TemporaryMessage;
}
