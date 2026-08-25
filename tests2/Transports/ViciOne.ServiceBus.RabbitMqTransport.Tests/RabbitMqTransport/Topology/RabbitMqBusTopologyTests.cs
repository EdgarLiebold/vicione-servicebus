using ViciOne.ServiceBus.RabbitMqTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqTransport.Topology;

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

    private static void AssertDestinationUsesConfiguredHost(Uri destination, Uri configuredHost)
    {
        var endpoint = new RabbitMqEndpointAddress(configuredHost, destination);

        Assert.Equal("rabbitmqs", endpoint.Scheme);
        Assert.Equal("broker.example.test", endpoint.Host);
        Assert.Equal(25671, endpoint.Port);
        Assert.Equal("production/client", endpoint.VirtualHost);
    }

    private sealed record OrderSubmitted;
}
