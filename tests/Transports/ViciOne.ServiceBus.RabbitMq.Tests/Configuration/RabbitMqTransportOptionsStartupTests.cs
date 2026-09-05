using System.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.Configuration;

public sealed class RabbitMqTransportOptionsStartupTests
{
    [Theory]
    [InlineData(InvalidOption.Host, "Host")]
    [InlineData(InvalidOption.Port, "Port")]
    [InlineData(InvalidOption.ManagementPort, "ManagementPort")]
    [InlineData(InvalidOption.VirtualHost, "VHost")]
    [InlineData(InvalidOption.Credentials, "User and Pass")]
    [InlineData(InvalidOption.TlsProtocol, "Protocol")]
    [InlineData(InvalidOption.CertificatePassphrase, "CertPassphrase")]
    public void InvalidOption_FailsBeforeAnyBrokerConnection(InvalidOption invalid, string property)
    {
        using ServiceProvider provider = Provider(invalid);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        string failure = string.Join(Environment.NewLine, exception.Failures);
        Assert.Contains(property, failure, StringComparison.Ordinal);
        Assert.Contains("RabbitMQ", failure, StringComparison.Ordinal);
    }

    [Fact]
    public void CoherentDefaults_PassTheSameStartupBoundary()
    {
        using ServiceProvider provider = Provider(null);

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    static ServiceProvider Provider(InvalidOption? invalid)
    {
        var services = new ServiceCollection();
        services.Configure<RabbitMqTransportOptions>(options =>
        {
            switch (invalid)
            {
                case InvalidOption.Host: options.Host = " "; break;
                case InvalidOption.Port: options.Port = 0; break;
                case InvalidOption.ManagementPort: options.ManagementPort = 0; break;
                case InvalidOption.VirtualHost: options.VHost = " "; break;
                case InvalidOption.Credentials: options.User = null!; break;
            }
        });
        services.Configure<RabbitMqSslOptions>(options =>
        {
            if (invalid == InvalidOption.TlsProtocol)
                options.Protocol = (SslProtocols)int.MaxValue;
            if (invalid == InvalidOption.CertificatePassphrase)
                options.CertPassphrase = "secret";
        });
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingRabbitMq();
        });
        return services.BuildServiceProvider();
    }

    public enum InvalidOption
    {
        Host,
        Port,
        ManagementPort,
        VirtualHost,
        Credentials,
        TlsProtocol,
        CertificatePassphrase,
    }
}
