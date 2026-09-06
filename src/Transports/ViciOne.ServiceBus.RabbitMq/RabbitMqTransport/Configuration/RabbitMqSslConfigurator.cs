using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Copies and updates RabbitMQ TLS certificate, protocol, and validation settings.</summary>
public class RabbitMqSslConfigurator :
    IRabbitMqSslConfigurator
{
    /// <summary>Creates a TLS configurator from current host settings.</summary>
    /// <param name="settings">The RabbitMQ host settings to copy.</param>
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

    /// <summary>Gets or sets the acceptable policy errors.</summary>
    public SslPolicyErrors AcceptablePolicyErrors { get; set; }

    /// <summary>Adds server-certificate policy errors to the allowed set.</summary>
    /// <param name="policyErrors">The policy-error flags to allow.</param>
    public void AllowPolicyErrors(SslPolicyErrors policyErrors)
    {
        AcceptablePolicyErrors |= policyErrors;
    }

    /// <summary>Removes server-certificate policy errors from the allowed set.</summary>
    /// <param name="policyErrors">The policy-error flags to enforce.</param>
    public void EnforcePolicyErrors(SslPolicyErrors policyErrors)
    {
        AcceptablePolicyErrors &= ~policyErrors;
    }

    /// <summary>Gets or sets the client-certificate file path.</summary>
    public string? CertificatePath { get; set; }

    /// <summary>Gets or sets the client-certificate passphrase.</summary>
    public string? CertificatePassphrase { get; set; }

    /// <summary>Gets or sets the client certificate supplied directly to RabbitMQ.Client.</summary>
    public X509Certificate? Certificate { get; set; }

    /// <summary>Gets or sets the expected broker certificate name.</summary>
    public string? ServerName { get; set; }

    /// <summary>Gets or sets the allowed TLS protocol versions.</summary>
    public SslProtocols Protocol { get; set; }

    /// <summary>Gets or sets whether the client certificate supplies the RabbitMQ authentication identity.</summary>
    public bool UseCertificateAsAuthenticationIdentity { get; set; }

    /// <summary>Gets or sets the client-certificate selection callback.</summary>
    public LocalCertificateSelectionCallback? CertificateSelectionCallback { get; set; }

    /// <summary>Gets or sets the server-certificate validation callback.</summary>
    public RemoteCertificateValidationCallback? CertificateValidationCallback { get; set; }
}
