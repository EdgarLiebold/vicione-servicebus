using System;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Builds RabbitMQ connection settings from a URI or explicit host components.</summary>
public class RabbitMqHostConfigurator :
    IRabbitMqHostConfigurator
{
    static readonly char[] _pathSeparator = ['/'];
    readonly ConfigurationHostSettings _settings;

    /// <summary>Creates host settings by parsing a RabbitMQ transport URI.</summary>
    /// <param name="hostAddress">The RabbitMQ host and virtual-host URI.</param>
    /// <param name="connectionName">An optional client-provided connection name.</param>
    public RabbitMqHostConfigurator(Uri hostAddress, string? connectionName = null)
    {
        _settings = hostAddress.GetConfigurationHostSettings();

        if (_settings.Ssl)
            UseSsl();

        _settings.VirtualHost = Uri.UnescapeDataString(GetVirtualHost(hostAddress));

        if (!string.IsNullOrEmpty(connectionName))
            _settings.ClientProvidedName = connectionName;
    }

    /// <summary>Creates host settings from explicit connection components.</summary>
    /// <param name="host">The broker host name.</param>
    /// <param name="virtualHost">The RabbitMQ virtual host.</param>
    /// <param name="port">The AMQP port.</param>
    /// <param name="connectionName">An optional client-provided connection name.</param>
    public RabbitMqHostConfigurator(string host, string virtualHost, ushort port = 5672, string? connectionName = null)
    {
        _settings = new ConfigurationHostSettings
        {
            Host = host,
            Port = port,
            VirtualHost = virtualHost
        };

        if (!string.IsNullOrEmpty(connectionName))
            _settings.ClientProvidedName = connectionName;
    }

    /// <summary>Gets the mutable settings configured by this instance.</summary>
    public RabbitMqHostSettings Settings => _settings;

    /// <summary>
    /// Sets whether RabbitMQ publisher confirmations are enabled. Durable Sender dispatches require confirmations.
    /// </summary>
    public bool PublisherConfirmation
    {
        set => _settings.PublisherConfirmation = value;
    }

    /// <summary>Enables TLS and applies optional certificate and protocol settings.</summary>
    /// <param name="configure">An optional RabbitMQ TLS configuration callback.</param>
    public void UseSsl(Action<IRabbitMqSslConfigurator>? configure = null)
    {
        _settings.EnsureAddressMutable();
        var configurator = new RabbitMqSslConfigurator(_settings);

        configure?.Invoke(configurator);

        _settings.Ssl = true;
        _settings.ClientCertificatePassphrase = configurator.CertificatePassphrase;
        _settings.ClientCertificatePath = configurator.CertificatePath;
        _settings.ClientCertificate = configurator.Certificate;
        _settings.UseClientCertificateAsAuthenticationIdentity = configurator.UseCertificateAsAuthenticationIdentity;
        _settings.AcceptablePolicyErrors = configurator.AcceptablePolicyErrors;
        _settings.SslServerName = configurator.ServerName ?? _settings.Host;
        _settings.SslProtocol = configurator.Protocol;
        _settings.CertificateSelectionCallback = configurator.CertificateSelectionCallback;
        _settings.CertificateValidationCallback = configurator.CertificateValidationCallback;
    }

    /// <summary>Configures the continuation timeout.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    public void ContinuationTimeout(TimeSpan timeout)
    {
        _settings.ContinuationTimeout = timeout;
    }

    /// <summary>Configures the maximum message size.</summary>
    /// <param name="maxMessageSize">The maximum inbound message-body size in bytes.</param>
    public void MaxMessageSize(uint maxMessageSize)
    {
        _settings.MaxMessageSize = maxMessageSize;
    }

    /// <summary>Sets the callback invoked before each connection attempt to refresh client-factory settings.</summary>
    public RefreshConnectionFactoryCallback OnRefreshConnectionFactory
    {
        set => _settings.OnRefreshConnectionFactory = value;
    }

    /// <summary>Sets the requested RabbitMQ connection heartbeat in seconds.</summary>
    /// <param name="requestedHeartbeat">The heartbeat interval in seconds.</param>
    public void Heartbeat(ushort requestedHeartbeat)
    {
        _settings.Heartbeat = TimeSpan.FromSeconds(requestedHeartbeat);
    }

    /// <summary>Sets the requested RabbitMQ connection heartbeat.</summary>
    /// <param name="timeSpan">The heartbeat interval.</param>
    public void Heartbeat(TimeSpan timeSpan)
    {
        _settings.Heartbeat = timeSpan;
    }

    /// <summary>Sets the RabbitMQ user name.</summary>
    /// <param name="username">The user name passed to the client.</param>
    public void Username(string username)
    {
        _settings.Username = username;
    }

    /// <summary>Sets the RabbitMQ connection password.</summary>
    /// <param name="password">The password passed to the client.</param>
    public void Password(string password)
    {
        _settings.Password = password;
    }

    /// <summary>Sets a dynamic credentials provider instead of fixed user-name/password credentials.</summary>
    public ICredentialsProvider CredentialsProvider
    {
        set => _settings.CredentialsProvider = value;
    }

    /// <summary>Configures cluster nodes used when establishing connections.</summary>
    /// <param name="configureCluster">The callback that adds RabbitMQ cluster nodes.</param>
    public void UseCluster(Action<IRabbitMqClusterConfigurator> configureCluster)
    {
        var configurator = new RabbitMqClusterConfigurator(_settings);
        configureCluster(configurator);

        _settings.EndpointResolver = configurator.GetEndpointResolver();
    }

    /// <summary>Configures the requested channel limit.</summary>
    /// <param name="value">The requested maximum number of AMQP channels.</param>
    public void RequestedChannelMax(ushort value)
    {
        _settings.RequestedChannelMax = value;
    }

    /// <summary>Configures the requested frame-size limit.</summary>
    /// <param name="value">The requested maximum AMQP frame size in bytes.</param>
    public void RequestedFrameMax(uint value)
    {
        _settings.RequestedFrameMax = value;
    }

    /// <summary>Configures the requested connection timeout.</summary>
    /// <param name="milliseconds">The connection timeout in milliseconds.</param>
    public void RequestedConnectionTimeout(int milliseconds)
    {
        _settings.RequestedConnectionTimeout = TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>Configures the requested connection timeout.</summary>
    /// <param name="timeSpan">The connection timeout.</param>
    public void RequestedConnectionTimeout(TimeSpan timeSpan)
    {
        _settings.RequestedConnectionTimeout = timeSpan;
    }

    /// <summary>Sets the client-provided connection name shown by RabbitMQ.</summary>
    /// <param name="connectionName">The connection name, or <see langword="null" /> to clear it.</param>
    public void ConnectionName(string? connectionName)
    {
        _settings.ClientProvidedName = connectionName;
    }

    string GetVirtualHost(Uri address)
    {
        var segments = address.AbsolutePath.Split(_pathSeparator, StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
            return "/";

        if (segments.Length == 1)
            return segments[0];

        throw new FormatException("The host path must be empty or contain a single virtual host name");
    }
}
