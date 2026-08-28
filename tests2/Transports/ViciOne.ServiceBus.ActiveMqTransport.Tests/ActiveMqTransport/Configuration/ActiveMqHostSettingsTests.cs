using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.Tests.ActiveMqTransport.Configuration;

public sealed class ActiveMqHostSettingsTests
{
    [Theory]
    [InlineData("activemq", "tcp", "activemq:failover:")]
    [InlineData("amqp", "amqp", "failover:")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-CONFIGURATION", "failover-uri-canonical-and-ipv6-safe")]
    public void FailoverUri_HasCanonicalPrecedenceForOpenWireAndAmqp(
        string hostScheme,
        string brokerScheme,
        string expectedPrefix)
    {
        ActiveMqHostConfigurator configurator = new(new Uri($"{hostScheme}://primary:61616"));
        configurator.FailoverHosts(
            new Uri($"{brokerScheme}://failover-one:61616"),
            new Uri($"{brokerScheme}://[2001:db8::1]:26161"));
        configurator.TransportOptions(
        [
            new KeyValuePair<string, string>("transport.sendBufferSize", "10=bytes"),
            new KeyValuePair<string, string>("reconnectAttempts", "-1"),
        ]);

        Uri brokerAddress = configurator.Settings.BrokerAddress;
        string value = brokerAddress.OriginalString;

        Assert.StartsWith(expectedPrefix, value, StringComparison.Ordinal);
        Assert.Contains("failover-one:61616", value, StringComparison.Ordinal);
        Assert.Contains("[2001:db8::1]:26161", value, StringComparison.Ordinal);
        Assert.Contains("transport.sendBufferSize=10%3Dbytes", value, StringComparison.Ordinal);
        Assert.DoesNotContain("primary", value, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-CONFIGURATION", "failover-protocol-must-match")]
    public void FailoverEndpoint_RejectsAProtocolThatCannotBeUsedByTheSelectedProvider()
    {
        ActiveMqHostConfigurator configurator = new(new Uri("activemq://primary:61616"));
        configurator.FailoverHosts(new Uri("amqp://wrong-provider:61616"));

        Assert.Throws<ActiveMqTransportConfigurationException>(() => _ = configurator.Settings.BrokerAddress);
    }

    [Theory]
    [InlineData("activemq", "tcp")]
    [InlineData("amqp", "amqp")]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-CONFIGURATION", "failover-port-must-be-explicit")]
    public void FailoverEndpoint_RejectsAnAddressWithoutAnExplicitPort(string hostScheme, string failoverScheme)
    {
        ActiveMqHostConfigurator configurator = new(new Uri($"{hostScheme}://primary:61616"));
        configurator.FailoverHosts(new Uri($"{failoverScheme}://secondary"));

        Assert.Throws<ActiveMqTransportConfigurationException>(() => _ = configurator.Settings.BrokerAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-CONFIGURATION", "tls-does-not-rewrite-explicit-port")]
    public void UseSsl_DoesNotRewriteTheExplicitPort()
    {
        ActiveMqHostConfigurator configurator = new(new Uri("activemq://broker:61616"));

        configurator.UseSsl();

        Assert.True(configurator.Settings.UseSsl);
        Assert.Equal(61616, configurator.Settings.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-CONFIGURATION", "duplicate-option-fails-fast")]
    public void TransportOptions_RejectDuplicateKeysInsteadOfSilentlyOverwriting()
    {
        ActiveMqHostConfigurator configurator = new(new Uri("amqp://primary:5672"));

        ArgumentException exception = Assert.Throws<ArgumentException>(() => configurator.TransportOptions(
        [
            new KeyValuePair<string, string>("nms.AsyncSend", "true"),
            new KeyValuePair<string, string>("nms.AsyncSend", "false"),
        ]));

        Assert.Contains("more than once", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HOST-CONFIGURATION", "bus-owns-immutable-settings-snapshot")]
    public void BusConfiguration_OwnsAnImmutableSnapshotOfMutableInputSettings()
    {
        var mutable = new OpenWireHostSettings(new Uri("activemq://broker:61616/production"))
        {
            Port = 61616,
            Username = "service",
            Password = "first-secret",
        };
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var bus = new ActiveMqBusConfiguration(topology);
        var configurator = new ActiveMqBusFactoryConfigurator(bus);
        configurator.Host(mutable);

        mutable.Port = 26161;
        mutable.Username = "mutated";
        mutable.Password = "second-secret";
        mutable.TransportOptions["nms.AsyncSend"] = "true";

        ActiveMqHostSettings snapshot = bus.HostConfiguration.Settings;
        Assert.Equal(61616, snapshot.Port);
        Assert.Equal("service", snapshot.Username);
        Assert.Equal("first-secret", snapshot.Password);
        Assert.Equal("production", snapshot.VirtualHost);
        Assert.DoesNotContain("nms.AsyncSend", snapshot.BrokerAddress.OriginalString, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", snapshot.HostAddress.OriginalString, StringComparison.OrdinalIgnoreCase);
    }
}
