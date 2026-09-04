using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Topology;

public sealed class RabbitMqBusTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "topology-uses-final-host-settings")]
    public void DestinationAddresses_UseTheFinalConfiguredHostForBothPublicOverloads()
    {
        var topologyConfiguration = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topologyConfiguration);
        IRabbitMqHostConfiguration hostConfiguration = busConfiguration.HostConfiguration;
        hostConfiguration.Settings = new ConfigurationHostSettings
        {
            Host = "broker.example.test",
            Port = 25671,
            VirtualHost = "production/client",
            Ssl = true,
        };

        Uri namedDestination = hostConfiguration.Topology.GetDestinationAddress("orders");
        Uri typedDestination = hostConfiguration.Topology.GetDestinationAddress(typeof(OrderSubmitted));

        AssertDestinationUsesConfiguredHost(namedDestination, hostConfiguration.HostAddress);
        AssertDestinationUsesConfiguredHost(typedDestination, hostConfiguration.HostAddress);
        Assert.Equal("orders", new RabbitMqEndpointAddress(hostConfiguration.HostAddress, namedDestination).Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "topology-preserves-message-lifetime")]
    public void TypedDestination_PreservesTemporaryAndDurableMessageLifetimes()
    {
        var topologyConfiguration = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topologyConfiguration);

        Uri temporaryDestination = busConfiguration.HostConfiguration.Topology.GetDestinationAddress(typeof(OrderSubmitted));
        Uri durableDestination = busConfiguration.HostConfiguration.Topology.GetDestinationAddress(typeof(PublicOrderSubmitted));

        Assert.False(typeof(OrderSubmitted).IsVisible);
        Assert.True(typeof(PublicOrderSubmitted).IsVisible);

        var temporaryEndpoint = new RabbitMqEndpointAddress(busConfiguration.HostConfiguration.HostAddress, temporaryDestination);
        Assert.False(temporaryEndpoint.Durable);
        Assert.True(temporaryEndpoint.AutoDelete);

        var durableEndpoint = new RabbitMqEndpointAddress(busConfiguration.HostConfiguration.HostAddress, durableDestination);
        Assert.True(durableEndpoint.Durable);
        Assert.False(durableEndpoint.AutoDelete);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "topology-treats-exchange-name-as-data")]
    public void NamedDestination_RejectsUriSyntaxInsteadOfInterpretingItAsConfiguration()
    {
        var topologyConfiguration = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topologyConfiguration);
        var formattedTopology = new RabbitMqBusTopology(
            busConfiguration.HostConfiguration,
            new FixedMessageNameFormatter("orders#fragment"),
            topologyConfiguration);

        var namedException = Assert.Throws<RabbitMqAddressException>(
            () => busConfiguration.HostConfiguration.Topology.GetDestinationAddress("orders?temporary=true"));
        var typedException = Assert.Throws<RabbitMqAddressException>(
            () => formattedTopology.GetDestinationAddress(typeof(OrderSubmitted)));

        Assert.Contains("entity name", namedException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("entity name", typedException.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertDestinationUsesConfiguredHost(Uri destination, Uri configuredHost)
    {
        var endpoint = new RabbitMqEndpointAddress(configuredHost, destination);

        Assert.Equal("rabbitmqs", endpoint.Scheme);
        Assert.Equal("broker.example.test", endpoint.Host);
        Assert.Equal(25671, endpoint.Port);
        Assert.Equal("production/client", endpoint.VirtualHost);
    }

    private sealed record OrderSubmitted;

    private sealed class FixedMessageNameFormatter(string messageName) : IMessageNameFormatter
    {
        public string GetMessageName(Type type) => messageName;
    }
}

public sealed record PublicOrderSubmitted;
