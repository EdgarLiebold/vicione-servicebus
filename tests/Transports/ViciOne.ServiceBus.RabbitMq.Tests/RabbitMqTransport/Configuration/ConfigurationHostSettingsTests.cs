using System.Net.Security;
using System.Security.Authentication;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class ConfigurationHostSettingsTests
{
    [Theory]
    [InlineData(false, 25672, "rabbitmq://broker:25672/production")]
    [InlineData(true, 25671, "rabbitmqs://broker:25671/production")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "transport-security-selects-scheme")]
    public void HostAddress_UsesConfiguredTransportSecurity(bool useTls, int port, string expectedAddress)
    {
        var settings = new ConfigurationHostSettings
        {
            Host = "broker",
            Port = port,
            VirtualHost = "production",
            Ssl = useTls
        };

        Assert.Equal(new Uri(expectedAddress), settings.HostAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "host-address-tracks-later-tls-configuration")]
    public void HostAddress_ReflectsTlsEnabledAfterAnEarlierAddressRead()
    {
        var configurator = new RabbitMqHostConfigurator("broker", "production");

        Assert.Equal(new Uri("rabbitmq://broker/production"), configurator.Settings.HostAddress);

        configurator.UseSsl();

        Assert.Equal(new Uri("rabbitmqs://broker:5672/production"), configurator.Settings.HostAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "receive-address-tracks-later-tls-configuration")]
    public void ReceiveInputAddress_ReflectsTlsEnabledAfterAnEarlierAddressRead()
    {
        var configurator = new RabbitMqHostConfigurator("broker", "production");
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topology);
        busConfiguration.HostConfiguration.Settings = configurator.Settings;
        IRabbitMqReceiveEndpointConfiguration endpoint =
            busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration("orders");

        Assert.Equal("rabbitmq", endpoint.InputAddress.Scheme);

        configurator.UseSsl();

        Assert.Equal("rabbitmqs", busConfiguration.HostConfiguration.HostAddress.Scheme);
        Assert.Equal("rabbitmqs", endpoint.InputAddress.Scheme);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "built-address-rejects-later-tls-configuration")]
    public void BuiltHost_RejectsTlsChangesThatWouldInvalidateRuntimeAddresses()
    {
        var configurator = new RabbitMqHostConfigurator("broker", "production");
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topology);
        busConfiguration.HostConfiguration.Settings = configurator.Settings;
        IRabbitMqReceiveEndpointConfiguration endpoint =
            busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration("orders");
        _ = busConfiguration.HostConfiguration.Build();

        Uri hostAddress = busConfiguration.HostConfiguration.HostAddress;
        Uri inputAddress = endpoint.InputAddress;

        Assert.Throws<InvalidOperationException>(() => configurator.UseSsl());
        Assert.Equal(hostAddress, busConfiguration.HostConfiguration.HostAddress);
        Assert.Equal(inputAddress, endpoint.InputAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "os-selected-tls-protocol")]
    public void DefaultTlsProtocol_IsSelectedByTheOperatingSystem()
    {
        var settings = new ConfigurationHostSettings();

        Assert.Equal(SslProtocols.None, settings.SslProtocol);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "strict-certificate-validation-by-default")]
    public void DefaultTlsPolicy_RejectsAllCertificatePolicyErrors()
    {
        var settings = new ConfigurationHostSettings();

        Assert.Equal(SslPolicyErrors.None, settings.AcceptablePolicyErrors);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "default-server-name-indication")]
    public void SslOptions_DefaultTheCertificateServerNameToTheConfiguredHost()
    {
        var settings = new ConfigurationHostSettings
        {
            Host = "broker.example.test",
            Ssl = true
        };
        var ssl = new SslOption();

        settings.ApplySslOptions(ssl);

        Assert.Equal("broker.example.test", ssl.ServerName);
        Assert.Equal(SslPolicyErrors.None, ssl.AcceptablePolicyErrors);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "plain-custom-constructor-does-not-infer-tls")]
    public void PlainHostConfiguration_DoesNotInferTlsFromPort5671()
    {
        var configurator = new RabbitMqHostConfigurator("broker", "production", 5671);

        Assert.False(configurator.Settings.Ssl);
        Assert.Equal(new Uri("rabbitmq://broker:5671/production"), configurator.Settings.HostAddress);
    }

    [Theory]
    [InlineData(1023, true)]
    [InlineData(1024, false)]
    [InlineData(262144, false)]
    [InlineData(262145, true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "batch-size-validation-uses-byte-limit")]
    public void BatchSizeValidation_UsesTheConfiguredSizeLimit(int sizeLimit, bool expectsFailure)
    {
        var topology = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var busConfiguration = new RabbitMqBusConfiguration(topology);
        var settings = new ConfigurationHostSettings();
        settings.ConfigureBatch(batch =>
        {
            batch.Enabled = true;
            batch.SizeLimit = sizeLimit;
        });
        busConfiguration.HostConfiguration.Settings = settings;

        bool hasBatchSizeFailure = busConfiguration.HostConfiguration.Validate()
            .Any(static result => result.Key == "BatchSizeLimit");

        Assert.Equal(expectsFailure, hasBatchSizeFailure);
    }

    [Theory]
    [InlineData("user", "broker", -1, null, null, "user@broker/")]
    [InlineData(null, "broker", 5678, "/production", null, "broker:5678/production")]
    [InlineData("configured", "logical", 5678, "production", "actual:5679", "configured@logical(actual:5679):5678/production")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "sanitized-connection-description")]
    public void ConnectionDescription_FormatsConfiguredAndSelectedHostsWithoutPasswords(
        string? username,
        string host,
        int port,
        string? virtualHost,
        string? actualHost,
        string expected)
    {
        var settings = new ConfigurationHostSettings
        {
            Username = username,
            Password = "must-not-appear",
            Host = host,
            Port = port,
            VirtualHost = virtualHost,
        };
        if (actualHost != null)
        {
            var resolver = new SequentialEndpointResolver([ClusterNode.Parse(actualHost)], settings);
            _ = resolver.All().Single();
            settings.EndpointResolver = resolver;
        }

        string description = settings.ToDescription();

        Assert.Equal(expected, description);
        Assert.DoesNotContain(settings.Password, description, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "factory-username-precedes-stale-settings")]
    public void ConnectionDescription_UsesTheRefreshedFactoryUsername()
    {
        var settings = new ConfigurationHostSettings
        {
            Username = "stale-user",
            Host = "broker",
            Port = -1,
            VirtualHost = "production",
        };
        var factory = new ConnectionFactory { UserName = "refreshed-user" };

        string description = settings.ToDescription(factory);

        Assert.Equal("refreshed-user@broker/production", description);
        Assert.DoesNotContain("stale-user", description, StringComparison.Ordinal);
    }
}
