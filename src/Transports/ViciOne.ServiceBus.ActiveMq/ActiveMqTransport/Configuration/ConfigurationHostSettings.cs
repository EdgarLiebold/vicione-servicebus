using System;
using System.Collections.Generic;
using System.Linq;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Builds validated Apache NMS broker and failover addresses from ActiveMQ host settings.</summary>
public abstract class ConfigurationHostSettings :
    ActiveMqHostSettings
{
    // Failover transport parameters are encoded separately from broker transport parameters.
    static readonly HashSet<string> _failoverArguments =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "backup",
            "initialReconnectDelay",
            "maxCacheSize",
            "maxReconnectAttempts",
            "maxReconnectDelay",
            "randomize",
            "reconnectDelayExponent",
            "reconnectSupported",
            "startupMaxReconnectAttempts",
            "timeout",
            "trackMessages",
            "updateURIsSupported",
            "updateURIsURL",
            "useExponentialBackOff",
            "warnAfterReconnectAttempts",
            "ha",
            "reconnectAttempts",
            "priorityBackup",
            "priorityURIs"
        };

    /// <summary>Initializes provider settings from a validated ActiveMQ host address.</summary>
    /// <param name="address">The broker host address.</param>
    protected ConfigurationHostSettings(Uri address)
    {
        var hostAddress = new ActiveMqHostAddress(address);

        Host = hostAddress.Host;
        Port = hostAddress.Port;
        VirtualHost = hostAddress.VirtualHost;

        Username = "";
        Password = "";

        TransportOptions = new Dictionary<string, string>();
        FailoverHosts = Array.Empty<Uri>();
    }

    /// <summary>Gets or sets the alternate broker addresses used for provider failover.</summary>
    public IReadOnlyList<Uri> FailoverHosts { get; set; }
    /// <summary>Gets the native Apache NMS URI options.</summary>
    public Dictionary<string, string> TransportOptions { get; }

    /// <summary>Gets the native scheme used for an individual broker URI.</summary>
    public abstract string HostScheme { get; }

    /// <summary>Gets the native failover URI scheme.</summary>
    public abstract string FailoverScheme { get; }

    /// <summary>Gets the native scheme used for the primary broker URI.</summary>
    public abstract string Scheme { get; }

    /// <summary>Gets the base Apache NMS provider scheme.</summary>
    public abstract string NmsScheme { get; }

    /// <summary>Gets the prefix applied to connection-wide failover options.</summary>
    public abstract string FailoverConnectionSettingPrefix { get; }

    /// <summary>Gets the primary broker host name.</summary>
    public string Host { get; }
    /// <summary>Gets or sets the primary broker port.</summary>
    public int Port { get; set; }
    /// <summary>Gets the configured broker namespace path.</summary>
    public string VirtualHost { get; }
    /// <summary>Gets or sets the broker user name.</summary>
    public string Username { get; set; }
    /// <summary>Gets or sets the broker password.</summary>
    public string Password { get; set; }
    /// <summary>Gets or sets whether the native provider uses TLS.</summary>
    public bool UseSsl { get; set; }

    /// <summary>Gets the canonical service-bus host address without credentials or native options.</summary>
    public Uri HostAddress => FormatHostAddress();
    /// <summary>Gets the native Apache NMS connection URI, including failover hosts and provider options.</summary>
    public Uri BrokerAddress => FormatBrokerAddress();

    /// <summary>Creates an Apache NMS connection for the configured broker address and credentials.</summary>
    /// <returns>The unstarted native connection.</returns>
    public IConnection CreateConnection()
    {
        var factory = new NMSConnectionFactory(BrokerAddress);
        if (string.IsNullOrEmpty(Username) && string.IsNullOrEmpty(Password))
            return factory.ConnectionFactory.CreateConnection();
        return factory.ConnectionFactory.CreateConnection(Username, Password);
    }

    Uri FormatHostAddress()
    {
        return new ActiveMqHostAddress(NmsScheme, Host, Port, VirtualHost);
    }

    Uri FormatBrokerAddress()
    {
        if (FailoverHosts.Count > 0)
        {
            // Each broker URI receives only transport-specific parameters.
            var failoverServerPart = GetQueryString(kv => !IsFailoverArgument(kv.Key));
            var failoverPart = string.Join(",", FailoverHosts
                .Select(failoverHost => FormatFailoverHost(failoverHost, failoverServerPart)));
            // Apache.NMS.ActiveMQ requires the "transport." prefix on failover parameters.
            var failoverQueryPart = GetQueryString(kv => IsFailoverArgument(kv.Key), FailoverConnectionSettingPrefix);
            return new Uri($"{FailoverScheme}:({failoverPart}){failoverQueryPart}");
        }

        var queryPart = GetQueryString(_ => true);
        var uri = new Uri($"{Scheme}://{Host}:{Port}{queryPart}");
        return uri;
    }

    string GetQueryString(Func<KeyValuePair<string, string>, bool> predicate, string prefix = "")
    {
        if (TransportOptions.Count == 0)
            return "";

        var queryString = string.Join("&", TransportOptions.Where(predicate)
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair =>
        {
            var key = pair.Key.StartsWith(prefix, StringComparison.Ordinal) ? pair.Key : $"{prefix}{pair.Key}";
            return $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(pair.Value)}";
        }));

        return $"?{queryString}";
    }

    /// <summary>Returns the primary TCP or TLS broker endpoint without credentials or provider options.</summary>
    /// <returns>The absolute broker endpoint string.</returns>
    public override string ToString()
    {
        return new UriBuilder
        {
            Scheme = UseSsl ? "ssl" : "tcp",
            Host = Host,
            Port = Port
        }.Uri.ToString();
    }

    static bool IsFailoverArgument(string key)
    {
        return key.StartsWith("nested.", StringComparison.OrdinalIgnoreCase) || _failoverArguments.Any(f => key.EndsWith(f, StringComparison.InvariantCulture));
    }

    string FormatFailoverHost(Uri address, string query)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (!address.IsAbsoluteUri || !string.Equals(address.Scheme, HostScheme, StringComparison.OrdinalIgnoreCase))
            throw new ActiveMqTransportConfigurationException($"The failover endpoint must use the '{HostScheme}' scheme.");
        if (string.IsNullOrWhiteSpace(address.Host))
            throw new ActiveMqTransportConfigurationException("The failover endpoint host must not be empty.");
        if (!string.IsNullOrEmpty(address.UserInfo) || !string.IsNullOrEmpty(address.Query) || !string.IsNullOrEmpty(address.Fragment)
            || address.AbsolutePath != "/"
            || address.IsDefaultPort
            || address.Port <= 0)
        {
            throw new ActiveMqTransportConfigurationException(
                "Failover endpoints contain only scheme, host, and an explicit port; credentials and options use the typed configurator.");
        }

        return new UriBuilder
        {
            Scheme = HostScheme,
            Host = address.Host,
            Port = address.Port,
            Query = query.TrimStart('?')
        }.Uri.ToString();
    }
}
