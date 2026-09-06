using System.Security.Authentication;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines named-options binding for RabbitMQ TLS settings.</summary>
public sealed class RabbitMqSslOptions
{
    /// <summary>Gets or sets the expected broker certificate name.</summary>
    public string? ServerName { get; set; }
    /// <summary>Gets or sets whether all server-certificate policy errors are allowed.</summary>
    public bool Trust { get; set; }
    /// <summary>Gets or sets the client-certificate file path.</summary>
    public string? CertPath { get; set; }
    /// <summary>Gets or sets the client-certificate passphrase.</summary>
    public string? CertPassphrase { get; set; }
    /// <summary>Gets or sets whether the client certificate supplies the RabbitMQ authentication identity.</summary>
    public bool CertIdentity { get; set; }
    /// <summary>Gets or sets the allowed TLS protocol versions.</summary>
    public SslProtocols Protocol { get; set; } = ConfigurationHostSettings.DefaultSslProtocols;
}
