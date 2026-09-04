using System.Security.Authentication;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines configuration options for rabbit mq ssl.
/// </summary>
public class RabbitMqSslOptions
{
    /// <summary>
    /// Gets or sets the server name value.
    /// </summary>
    public string? ServerName { get; set; }
    /// <summary>
    /// Gets or sets the trust value.
    /// </summary>
    public bool Trust { get; set; }
    /// <summary>
    /// Gets or sets the cert path value.
    /// </summary>
    public string? CertPath { get; set; }
    /// <summary>
    /// Gets or sets the cert passphrase value.
    /// </summary>
    public string? CertPassphrase { get; set; }
    /// <summary>
    /// Gets or sets the cert identity value.
    /// </summary>
    public bool CertIdentity { get; set; }
    /// <summary>
    /// Gets or sets the protocol value.
    /// </summary>
    public SslProtocols Protocol { get; set; } = ConfigurationHostSettings.DefaultSslProtocols;
}
