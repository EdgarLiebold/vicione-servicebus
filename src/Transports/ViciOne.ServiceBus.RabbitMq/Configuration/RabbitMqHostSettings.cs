using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Settings to configure a RabbitMQ host explicitly without requiring the fluent interface.</summary>
public interface RabbitMqHostSettings
{
    /// <summary>The RabbitMQ host to connect to (should be a valid hostname).</summary>
    string? Host { get; }

    /// <summary>The RabbitMQ AMQP port.</summary>
    int Port { get; }

    /// <summary>The virtual host for the connection.</summary>
    string? VirtualHost { get; }

    /// <summary>The user name passed to RabbitMQ when password authentication is used.</summary>
    string? Username { get; }

    /// <summary>Gets the password passed to the RabbitMQ client when username-and-password authentication is used.</summary>
    string? Password { get; }

    /// <summary>The requested connection heartbeat interval.</summary>
    TimeSpan Heartbeat { get; }

    /// <summary>Indicates whether the connection uses TLS.</summary>
    bool Ssl { get; }

    /// <summary>TLS protocol selection. The default is <see cref="SslProtocols.None" />, allowing the operating system to negotiate enabled secure protocols.</summary>
    SslProtocols SslProtocol { get; }

    /// <summary>The server name specified on the certificate for the RabbitMQ server.</summary>
    string? SslServerName { get; }

    /// <summary>The server-certificate policy errors the TLS connection is allowed to ignore.</summary>
    SslPolicyErrors AcceptablePolicyErrors { get; }

    /// <summary>The path to the client certificate if client certificate authentication is used.</summary>
    string? ClientCertificatePath { get; }

    /// <summary>The passphrase for the client certificate found using the <see cref="ClientCertificatePath" />, not required if <see cref="ClientCertificate" /> is populated.</summary>
    string? ClientCertificatePassphrase { get; }

    /// <summary>
    /// A certificate to use for client certificate authentication, if not set then the <see cref="ClientCertificatePath" /> and
    /// <see cref="ClientCertificatePassphrase" /> will be used.
    /// </summary>
    X509Certificate? ClientCertificate { get; }

    /// <summary>Whether the client certificate should be used for logging in to RabbitMQ, ignoring any username and password set.</summary>
    /// <remarks>
    /// RabbitMQ must be configured correctly for this to work, including enabling the rabbitmq_auth_mechanism_ssl plugin
    /// </remarks>
    bool UseClientCertificateAsAuthenticationIdentity { get; }

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

    /// <summary>The optional endpoint resolver used to select a cluster node for each connection attempt.</summary>
    IRabbitMqEndpointResolver? EndpointResolver { get; }

    /// <summary>The client-provided name for the connection (displayed in RabbitMQ admin panel).</summary>
    string? ClientProvidedName { get; }

    /// <summary>Gets the normalized RabbitMQ transport address.</summary>
    Uri HostAddress { get; }

    /// <summary>Indicates whether RabbitMQ publisher confirmations are enabled.</summary>
    bool PublisherConfirmation { get; }

    /// <summary>The maximum number of channels for the connection.</summary>
    ushort RequestedChannelMax { get; }

    /// <summary>Gets the requested connection timeout.</summary>
    TimeSpan RequestedConnectionTimeout { get; }

    /// <summary>Gets the client-side publish-batch settings.</summary>
    BatchSettings BatchSettings { get; }

    /// <summary>Gets the timeout for RabbitMQ client RPC continuations.</summary>
    TimeSpan ContinuationTimeout { get; }

    /// <summary>Gets the optional maximum inbound message-body size accepted by RabbitMQ.Client.</summary>
    uint? MaxMessageSize { get; }

    /// <summary>The credential provider, overriding the default username/password credentials.</summary>
    ICredentialsProvider? CredentialsProvider { get; }

    /// <summary>The requested maximum frame size.</summary>
    uint? RequestedFrameMax { get; }

    /// <summary>
    /// Refreshes mutable connection-factory settings immediately before a connection attempt.
    /// </summary>
    /// <param name="connectionFactory">The RabbitMQ client factory that will create the connection.</param>
    /// <param name="cancellationToken">Cancellation checked before invoking the refresh callback.</param>
    /// <returns>A task that completes when the factory settings have been refreshed.</returns>
    Task RefreshAsync(ConnectionFactory connectionFactory, CancellationToken cancellationToken = default);
}
