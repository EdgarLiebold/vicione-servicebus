using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Configures TLS for RabbitMQ connections. Broker-side guidance is available in the
/// <see href="https://www.rabbitmq.com/docs/ssl">RabbitMQ TLS documentation</see>.
/// </summary>
public interface IRabbitMqSslConfigurator
{
    /// <summary>The TLS protocol selection. Prefer <see cref="SslProtocols.None" /> so the operating system selects enabled secure protocols.</summary>
    SslProtocols Protocol { get; set; }

    /// <summary>The expected server name validated against the broker certificate.</summary>
    string? ServerName { get; set; }

    /// <summary>The path to a file containing a certificate to use for client authentication, not required if <see cref="Certificate" /> is populated.</summary>
    string? CertificatePath { get; set; }

    /// <summary>The password for the certificate file at <see cref="CertificatePath" />.</summary>
    string? CertificatePassphrase { get; set; }

    /// <summary>
    /// A certificate instance to use for client authentication, if provided then <see cref="CertificatePath" />
    /// and <see cref="CertificatePassphrase" /> are not required.
    /// </summary>
    X509Certificate? Certificate { get; set; }

    /// <summary>Specifies whether the client certificate also supplies the RabbitMQ authentication identity.</summary>
    bool UseCertificateAsAuthenticationIdentity { get; set; }

    /// <summary>
    /// An optional client-certificate selection callback. If this is not specified,
    /// the first valid certificate found will be used.
    /// </summary>
    LocalCertificateSelectionCallback? CertificateSelectionCallback { get; set; }

    /// <summary>
    /// An optional server-certificate validation callback. If this is not specified,
    /// the default callback will be used in conjunction with the <see cref="P:RabbitMQ.Client.SslOption.AcceptablePolicyErrors" /> property to
    /// determine if the remote server certificate is valid.
    /// </summary>
    RemoteCertificateValidationCallback? CertificateValidationCallback { get; set; }

    /// <summary>Adds certificate policy errors that the client is allowed to ignore.</summary>
    /// <param name="policyErrors">The policy-error flags to allow.</param>
    void AllowPolicyErrors(SslPolicyErrors policyErrors);

    /// <summary>Removes certificate policy errors from the allowed set.</summary>
    /// <param name="policyErrors">The policy-error flags that must be enforced.</param>
    void EnforcePolicyErrors(SslPolicyErrors policyErrors);
}
