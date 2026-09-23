using System.Net.Security;
using System.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.Configuration;

public sealed class RabbitMqRegistrationOptionsTests
{
    [Theory]
    [InlineData(true, SslPolicyErrors.RemoteCertificateNameMismatch
        | SslPolicyErrors.RemoteCertificateChainErrors
        | SslPolicyErrors.RemoteCertificateNotAvailable)]
    [InlineData(false, SslPolicyErrors.None)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-REGISTRATION-OPTIONS", "complete-transport-and-tls-projection")]
    public void RegisteredOptions_ProjectCompleteTransportAndTlsSettingsIntoTheBus(
        bool trust,
        SslPolicyErrors expectedPolicyErrors)
    {
        using ServiceProvider provider = CreateProvider(
            options =>
            {
                options.Host = "broker.internal";
                options.Port = 25_671;
                options.VHost = "/team/blue";
                options.User = "application-user";
                options.Pass = "application-secret";
                options.ConnectionName = "orders-service";
                options.UseSsl = true;
            },
            options =>
            {
                options.ServerName = "certificate.internal";
                options.Trust = trust;
                options.CertPath = "/certificates/client.pfx";
                options.CertPassphrase = "certificate-secret";
                options.CertIdentity = true;
                options.Protocol = SslProtocols.Tls13;
            });

        RabbitMqHostSettings settings = GetHostSettings(provider);
        ConnectionFactory client = settings.GetConnectionFactory();

        Assert.Equal("broker.internal", settings.Host);
        Assert.Equal(25_671, settings.Port);
        Assert.Equal("/team/blue", settings.VirtualHost);
        Assert.Equal("application-user", settings.Username);
        Assert.Equal("application-secret", settings.Password);
        Assert.Equal("orders-service", settings.ClientProvidedName);
        Assert.True(settings.Ssl);
        Assert.Equal("certificate.internal", settings.SslServerName);
        Assert.Equal("/certificates/client.pfx", settings.ClientCertificatePath);
        Assert.Equal("certificate-secret", settings.ClientCertificatePassphrase);
        Assert.True(settings.UseClientCertificateAsAuthenticationIdentity);
        Assert.Equal(SslProtocols.Tls13, settings.SslProtocol);
        Assert.Equal(expectedPolicyErrors, settings.AcceptablePolicyErrors);

        Assert.Equal(settings.Host, client.HostName);
        Assert.Equal(settings.Port, client.Port);
        Assert.Equal(settings.VirtualHost, client.VirtualHost);
        Assert.Equal("", client.UserName);
        Assert.Equal("", client.Password);
        Assert.IsType<ExternalMechanismFactory>(Assert.Single(client.AuthMechanisms));
        Assert.True(client.Ssl.Enabled);
        Assert.Equal(settings.SslServerName, client.Ssl.ServerName);
        Assert.Equal(settings.ClientCertificatePath, client.Ssl.CertPath);
        Assert.Equal(settings.ClientCertificatePassphrase, client.Ssl.CertPassphrase);
        Assert.Equal(settings.SslProtocol, client.Ssl.Version);
        Assert.Equal(settings.AcceptablePolicyErrors, client.Ssl.AcceptablePolicyErrors);
    }

    [Theory]
    [InlineData("application-user", "application-secret")]
    [InlineData("", "")]
    [InlineData("application-user", "")]
    [InlineData("", "application-secret")]
    [InlineData(" application-user ", " application-secret ")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CREDENTIALS", "registered-empty-values-remain-anonymous")]
    public void RegisteredCredentials_ReachTheClientWithoutFallback(string username, string password)
    {
        using ServiceProvider provider = CreateProvider(
            options =>
            {
                options.Host = "anonymous.internal";
                options.User = username;
                options.Pass = password;
            });

        RabbitMqHostSettings settings = GetHostSettings(provider);
        ConnectionFactory client = settings.GetConnectionFactory();

        Assert.Equal(username, settings.Username);
        Assert.Equal(password, settings.Password);
        Assert.False(settings.Ssl);
        Assert.Equal(username, client.UserName);
        Assert.Equal(password, client.Password);
        Assert.False(client.Ssl.Enabled);
    }

    static ServiceProvider CreateProvider(
        Action<RabbitMqTransportOptions> configureTransport,
        Action<RabbitMqSslOptions>? configureSsl = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.Configure(configureTransport);
        if (configureSsl != null)
            services.Configure(configureSsl);
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingRabbitMq();
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    static RabbitMqHostSettings GetHostSettings(ServiceProvider provider)
    {
        IBusInstance instance = provider.GetRequiredService<IBusInstance>();
        IRabbitMqHostConfiguration host = Assert.IsAssignableFrom<IRabbitMqHostConfiguration>(instance.HostConfiguration);
        return host.Settings;
    }
}
