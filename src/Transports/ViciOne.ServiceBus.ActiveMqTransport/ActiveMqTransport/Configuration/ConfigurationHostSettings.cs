using System;
using System.Collections.Generic;
using System.Linq;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration;

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

    public IReadOnlyList<Uri> FailoverHosts { get; set; }
    public Dictionary<string, string> TransportOptions { get; }

    public abstract string HostScheme { get; }

    public abstract string FailoverScheme { get; }

    public abstract string Scheme { get; }

    public abstract string NmsScheme { get; }

    public abstract string FailoverConnectionSettingPrefix { get; }

    public string Host { get; }
    public int Port { get; set; }
    public string VirtualHost { get; }
    public string Username { get; set; }
    public string Password { get; set; }
    public bool UseSsl { get; set; }

    public Uri HostAddress => FormatHostAddress();
    public Uri BrokerAddress => FormatBrokerAddress();

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
