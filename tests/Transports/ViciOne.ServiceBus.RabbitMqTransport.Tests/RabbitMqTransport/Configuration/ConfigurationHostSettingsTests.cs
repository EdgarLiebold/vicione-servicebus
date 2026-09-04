using System.Net.Security;
using System.Security.Authentication;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMqTransport.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqTransport.Configuration;

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
}
