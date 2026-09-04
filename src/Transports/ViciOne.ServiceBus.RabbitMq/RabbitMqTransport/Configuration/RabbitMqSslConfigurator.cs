using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq ssl configurator implementation.
/// </summary>
public class RabbitMqSslConfigurator :
    IRabbitMqSslConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public RabbitMqSslConfigurator(RabbitMqHostSettings settings)
    {
        CertificatePath = settings.ClientCertificatePath;
        CertificatePassphrase = settings.ClientCertificatePassphrase;
        Certificate = settings.ClientCertificate;
        UseCertificateAsAuthenticationIdentity = settings.UseClientCertificateAsAuthenticationIdentity;
        ServerName = settings.SslServerName;
        Protocol = settings.SslProtocol;
        AcceptablePolicyErrors = settings.AcceptablePolicyErrors;
        CertificateSelectionCallback = settings.CertificateSelectionCallback;
        CertificateValidationCallback = settings.CertificateValidationCallback;
    }

    /// <summary>
    /// Gets or sets the acceptable policy errors value.
    /// </summary>
    public SslPolicyErrors AcceptablePolicyErrors { get; set; }

    /// <summary>
    /// Performs the allow policy errors operation.
    /// </summary>
    /// <param name="policyErrors">The policy errors value.</param>
    public void AllowPolicyErrors(SslPolicyErrors policyErrors)
    {
        AcceptablePolicyErrors |= policyErrors;
    }

    /// <summary>
    /// Performs the enforce policy errors operation.
    /// </summary>
    /// <param name="policyErrors">The policy errors value.</param>
    public void EnforcePolicyErrors(SslPolicyErrors policyErrors)
    {
        AcceptablePolicyErrors &= ~policyErrors;
    }

    /// <summary>
    /// Gets or sets the certificate path value.
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the certificate passphrase value.
    /// </summary>
    public string? CertificatePassphrase { get; set; }

    /// <summary>
    /// Gets or sets the certificate value.
    /// </summary>
    public X509Certificate? Certificate { get; set; }

    /// <summary>
    /// Gets or sets the server name value.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Gets or sets the protocol value.
    /// </summary>
    public SslProtocols Protocol { get; set; }

    /// <summary>
    /// Gets or sets the use certificate as authentication identity value.
    /// </summary>
    public bool UseCertificateAsAuthenticationIdentity { get; set; }

    /// <summary>
    /// Gets or sets the certificate selection callback value.
    /// </summary>
    public LocalCertificateSelectionCallback? CertificateSelectionCallback { get; set; }

    /// <summary>
    /// Gets or sets the certificate validation callback value.
    /// </summary>
    public RemoteCertificateValidationCallback? CertificateValidationCallback { get; set; }
}
