using System;
using System.Collections.Generic;
using System.Linq;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides a configuration host settings implementation.
/// </summary>
public abstract class ConfigurationHostSettings :
    ActiveMqHostSettings
{
    // ActiveMQ Failover connection parameters https://activemq.apache.org/components/classic/documentation/failover-transport-reference
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
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

    /// <summary>
    /// Gets or sets the failover hosts value.
    /// </summary>
    public IReadOnlyList<Uri> FailoverHosts { get; set; }
    /// <summary>
    /// Gets the transport options value.
    /// </summary>
    public Dictionary<string, string> TransportOptions { get; }

    /// <summary>
    /// Gets the host scheme value.
    /// </summary>
    public abstract string HostScheme { get; }

    /// <summary>
    /// Gets the failover scheme value.
    /// </summary>
    public abstract string FailoverScheme { get; }

    /// <summary>
    /// Gets the scheme value.
    /// </summary>
    public abstract string Scheme { get; }

    /// <summary>
    /// Gets the nms scheme value.
    /// </summary>
    public abstract string NmsScheme { get; }

    /// <summary>
    /// Gets the failover connection setting prefix value.
    /// </summary>
    public abstract string FailoverConnectionSettingPrefix { get; }

    /// <summary>
    /// Gets the host value.
    /// </summary>
    public string Host { get; }
    /// <summary>
    /// Gets or sets the port value.
    /// </summary>
    public int Port { get; set; }
    /// <summary>
    /// Gets the virtual host value.
    /// </summary>
    public string VirtualHost { get; }
    /// <summary>
    /// Gets or sets the username value.
    /// </summary>
    public string Username { get; set; }
    /// <summary>
    /// Gets or sets the password value.
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// Gets or sets the use ssl value.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress => FormatHostAddress();
    /// <summary>
    /// Gets the broker address value.
    /// </summary>
    public Uri BrokerAddress => FormatBrokerAddress();

    /// <summary>
    /// Creates connection.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
        // create broker URI: http://activemq.apache.org/nms/activemq-uri-configuration.html
        if (FailoverHosts.Count > 0)
        {
            //filter only parameters which are not failover parameters
            var failoverServerPart = GetQueryString(kv => !IsFailoverArgument(kv.Key));
            var failoverPart = string.Join(",", FailoverHosts
                .Select(failoverHost => FormatFailoverHost(failoverHost, failoverServerPart)));
            //filter failover parameters only. Apache.NMS.ActiveMQ requires prefix "transport." for failover parameters
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

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
