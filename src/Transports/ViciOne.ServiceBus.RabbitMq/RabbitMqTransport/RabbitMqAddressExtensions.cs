using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides extension methods for rabbit mq address.
/// </summary>
public static class RabbitMqAddressExtensions
{
    /// <summary>
    /// Gets receive settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public static ReceiveSettings GetReceiveSettings(this Uri address)
    {
        var hostAddress = new RabbitMqHostAddress(address);
        var endpointAddress = new RabbitMqEndpointAddress(hostAddress, address);

        var topologyConfiguration = new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology());
        var endpointConfiguration = new RabbitMqEndpointConfiguration(topologyConfiguration);
        var settings = new RabbitMqReceiveSettings(endpointConfiguration, endpointAddress.Name, endpointAddress.ExchangeType,
            endpointAddress.Durable, endpointAddress.AutoDelete)
        {
            QueueName = endpointAddress.Name,
            Exclusive = endpointAddress.AutoDelete && !endpointAddress.Durable
        };

        if (hostAddress.Prefetch.HasValue)
            settings.PrefetchCount = hostAddress.Prefetch.Value;

        if (hostAddress.TimeToLive.HasValue)
            settings.QueueArguments.Add(RabbitMQ.Client.Headers.XMessageTTL, hostAddress.TimeToLive.Value);

        return settings;
    }

    /// <summary>
    /// Gets connection factory.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <returns>The result of the operation.</returns>
    public static ConnectionFactory GetConnectionFactory(this RabbitMqHostSettings settings, TimeProvider? timeProvider = null)
    {
        var factory = new ConnectionFactory
        {
            AutomaticRecoveryEnabled = false,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(1),
            TopologyRecoveryEnabled = false,
            HostName = settings.Host ?? "localhost",
            Port = settings.Port,
            VirtualHost = settings.VirtualHost ?? "/",
            RequestedHeartbeat = settings.Heartbeat == TimeSpan.Zero ? ConnectionFactory.DefaultHeartbeat : settings.Heartbeat,
            RequestedConnectionTimeout = settings.RequestedConnectionTimeout,
            RequestedChannelMax = settings.RequestedChannelMax,
            ContinuationTimeout = settings.ContinuationTimeout,
            HandshakeContinuationTimeout = settings.ContinuationTimeout
        };

        if (settings.MaxMessageSize.HasValue)
            factory.MaxInboundMessageBodySize = settings.MaxMessageSize.Value;
        if (settings.RequestedFrameMax.HasValue)
            factory.RequestedFrameMax = settings.RequestedFrameMax.Value;

        if (settings.EndpointResolver != null)
        {
            factory.HostName = "";
            factory.EndpointResolverFactory = x => settings.EndpointResolver;
        }

        if (settings.UseClientCertificateAsAuthenticationIdentity)
        {
            factory.AuthMechanisms = new List<IAuthMechanismFactory> { new ExternalMechanismFactory() };
            factory.UserName = "";
            factory.Password = "";
        }
        else if (settings.CredentialsProvider != null)
            factory.CredentialsProvider = settings.CredentialsProvider;
        else
        {
            if (!string.IsNullOrWhiteSpace(settings.Username))
                factory.UserName = settings.Username;

            if (!string.IsNullOrWhiteSpace(settings.Password))
                factory.Password = settings.Password;
        }

        ApplySslOptions(settings, factory.Ssl);

        factory.ClientProperties ??= new Dictionary<string, object?>();

        var hostInfo = HostMetadataCache.Host;

        factory.ClientProperties["client_api"] = "ViciOne.ServiceBus";
        factory.ClientProperties["ViciOneServiceBus_version"] = hostInfo.ViciOneServiceBusVersion;
        factory.ClientProperties["net_version"] = hostInfo.FrameworkVersion;
        factory.ClientProperties["hostname"] = hostInfo.MachineName;
        factory.ClientProperties["connected"] = (timeProvider ?? TimeProvider.System).GetLocalNow().ToString("R");
        factory.ClientProperties["process_id"] = hostInfo.ProcessId.ToString();
        factory.ClientProperties["process_name"] = hostInfo.ProcessName;
        if (hostInfo.Assembly != null)
            factory.ClientProperties["assembly"] = hostInfo.Assembly;

        if (hostInfo.AssemblyVersion != null)
            factory.ClientProperties["assembly_version"] = hostInfo.AssemblyVersion;

        return factory;
    }

    /// <summary>
    /// Performs the apply ssl options operation.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="option">The option value.</param>
    public static void ApplySslOptions(this RabbitMqHostSettings settings, SslOption option)
    {
        option.Enabled = settings.Ssl;
        option.Version = settings.SslProtocol;
        option.AcceptablePolicyErrors = settings.AcceptablePolicyErrors;
        option.ServerName = string.IsNullOrWhiteSpace(settings.SslServerName)
            ? settings.Host ?? ""
            : settings.SslServerName;
        option.Certs = settings.ClientCertificate == null ? null : new X509Certificate2Collection { settings.ClientCertificate };
        option.CertificateSelectionCallback = settings.CertificateSelectionCallback;
        option.CertificateValidationCallback = settings.CertificateValidationCallback;

        if (string.IsNullOrEmpty(settings.ClientCertificatePath))
        {
            option.CertPath = "";
            option.CertPassphrase = "";
        }
        else
        {
            option.CertPath = settings.ClientCertificatePath;
            option.CertPassphrase = settings.ClientCertificatePassphrase;
        }
    }

    /// <summary>
    /// Gets host settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public static RabbitMqHostSettings GetHostSettings(this Uri address)
    {
        return GetConfigurationHostSettings(address);
    }

    /// <summary>
    /// Gets rabbit mq host topology.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public static IRabbitMqBusTopology GetRabbitMqHostTopology(this IBus bus)
    {
        if (bus.Topology is IRabbitMqBusTopology hostTopology)
            return hostTopology;

        throw new ArgumentException("The bus is not a RabbitMQ bus", nameof(bus));
    }

    internal static ConfigurationHostSettings GetConfigurationHostSettings(this Uri address)
    {
        var hostAddress = new RabbitMqHostAddress(address);

        var hostSettings = new ConfigurationHostSettings
        {
            Host = hostAddress.Host,
            Ssl = RabbitMqHostAddress.IsSecureScheme(hostAddress.Scheme),
            VirtualHost = hostAddress.VirtualHost,
            Username = "",
            Password = ""
        };

        hostSettings.Port = hostAddress.Port;

        if (!string.IsNullOrEmpty(address.UserInfo))
        {
            var separator = address.UserInfo.IndexOf(':');
            if (separator < 0)
                hostSettings.Username = UriDecode(address.UserInfo);
            else
            {
                hostSettings.Username = UriDecode(address.UserInfo[..separator]);
                hostSettings.Password = UriDecode(address.UserInfo[(separator + 1)..]);
            }
        }

        hostSettings.Heartbeat = TimeSpan.FromSeconds(hostAddress.Heartbeat ?? 0);

        return hostSettings;
    }

    static string UriDecode(string uri)
    {
        return Uri.UnescapeDataString(uri.Replace("+", "%2B"));
    }

    /// <summary>
    /// Determines whether reply to address.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsReplyToAddress(this Uri address)
    {
        return address?.AbsolutePath?.EndsWith(RabbitMqExchangeNames.ReplyTo) ?? false;
    }
}
