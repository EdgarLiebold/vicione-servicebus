using System;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq host configurator implementation.
/// </summary>
public class RabbitMqHostConfigurator :
    IRabbitMqHostConfigurator
{
    static readonly char[] _pathSeparator = ['/'];
    readonly ConfigurationHostSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <param name="connectionName">The connection name value.</param>
    public RabbitMqHostConfigurator(Uri hostAddress, string? connectionName = null)
    {
        _settings = hostAddress.GetConfigurationHostSettings();

        if (_settings.Ssl)
            UseSsl();

        _settings.VirtualHost = Uri.UnescapeDataString(GetVirtualHost(hostAddress));

        if (!string.IsNullOrEmpty(connectionName))
            _settings.ClientProvidedName = connectionName;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="host">The host value.</param>
    /// <param name="virtualHost">The virtual host value.</param>
    /// <param name="port">The port value.</param>
    /// <param name="connectionName">The connection name value.</param>
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

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public RabbitMqHostSettings Settings => _settings;

    /// <summary>
    /// Gets or sets the publisher confirmation value.
    /// </summary>
    public bool PublisherConfirmation
    {
        set => _settings.PublisherConfirmation = value;
    }

    /// <summary>
    /// Configures ssl for the current pipeline.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void UseSsl(Action<IRabbitMqSslConfigurator>? configure = null)
    {
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

    /// <summary>
    /// Performs the continuation timeout operation.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    public void ContinuationTimeout(TimeSpan timeout)
    {
        _settings.ContinuationTimeout = timeout;
    }

    /// <summary>
    /// Performs the max message size operation.
    /// </summary>
    /// <param name="maxMessageSize">The max message size value.</param>
    public void MaxMessageSize(uint maxMessageSize)
    {
        _settings.MaxMessageSize = maxMessageSize;
    }

    /// <summary>
    /// Gets or sets the on refresh connection factory value.
    /// </summary>
    public RefreshConnectionFactoryCallback OnRefreshConnectionFactory
    {
        set => _settings.OnRefreshConnectionFactory = value;
    }

    /// <summary>
    /// Performs the heartbeat operation.
    /// </summary>
    /// <param name="requestedHeartbeat">The requested heartbeat value.</param>
    public void Heartbeat(ushort requestedHeartbeat)
    {
        _settings.Heartbeat = TimeSpan.FromSeconds(requestedHeartbeat);
    }

    /// <summary>
    /// Performs the heartbeat operation.
    /// </summary>
    /// <param name="timeSpan">The time span value.</param>
    public void Heartbeat(TimeSpan timeSpan)
    {
        _settings.Heartbeat = timeSpan;
    }

    /// <summary>
    /// Configures rname for the current pipeline.
    /// </summary>
    /// <param name="username">The username value.</param>
    public void Username(string username)
    {
        _settings.Username = username;
    }

    /// <summary>
    /// Performs the password operation.
    /// </summary>
    /// <param name="password">The password value.</param>
    public void Password(string password)
    {
        _settings.Password = password;
    }

    /// <summary>
    /// Gets or sets the credentials provider value.
    /// </summary>
    public ICredentialsProvider CredentialsProvider
    {
        set => _settings.CredentialsProvider = value;
    }

    /// <summary>
    /// Configures cluster for the current pipeline.
    /// </summary>
    /// <param name="configureCluster">The configure cluster value.</param>
    public void UseCluster(Action<IRabbitMqClusterConfigurator> configureCluster)
    {
        var configurator = new RabbitMqClusterConfigurator(_settings);
        configureCluster(configurator);

        _settings.EndpointResolver = configurator.GetEndpointResolver();
    }

    /// <summary>
    /// Performs the requested channel max operation.
    /// </summary>
    /// <param name="value">The value.</param>
    public void RequestedChannelMax(ushort value)
    {
        _settings.RequestedChannelMax = value;
    }

    /// <summary>
    /// Performs the requested frame max operation.
    /// </summary>
    /// <param name="value">The value.</param>
    public void RequestedFrameMax(uint value)
    {
        _settings.RequestedFrameMax = value;
    }

    /// <summary>
    /// Performs the requested connection timeout operation.
    /// </summary>
    /// <param name="milliseconds">The milliseconds value.</param>
    public void RequestedConnectionTimeout(int milliseconds)
    {
        _settings.RequestedConnectionTimeout = TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <summary>
    /// Performs the requested connection timeout operation.
    /// </summary>
    /// <param name="timeSpan">The time span value.</param>
    public void RequestedConnectionTimeout(TimeSpan timeSpan)
    {
        _settings.RequestedConnectionTimeout = timeSpan;
    }

    /// <summary>
    /// Connects ion name.
    /// </summary>
    /// <param name="connectionName">The connection name value.</param>
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
