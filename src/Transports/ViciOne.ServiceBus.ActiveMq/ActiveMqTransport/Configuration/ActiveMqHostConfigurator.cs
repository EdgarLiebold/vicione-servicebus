using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Configures credentials and provider options for an ActiveMQ broker.</summary>
public class ActiveMqHostConfigurator :
    IActiveMqHostConfigurator
{
    readonly ConfigurationHostSettings _settings;

    /// <summary>Creates provider-specific settings for an ActiveMQ or AMQP broker address.</summary>
    /// <param name="address">The validated broker address.</param>
    public ActiveMqHostConfigurator(Uri address)
    {
        switch (address.Scheme.ToLowerInvariant())
        {
            case ActiveMqHostAddress.AmqpScheme:
                _settings = new AmqpHostSettings(address);
                break;
            default:
                _settings = new OpenWireHostSettings(address);
                break;
        }
    }

    /// <summary>Gets the mutable provider-specific host settings.</summary>
    public ActiveMqHostSettings Settings => _settings;

    /// <summary>Sets the broker user name.</summary>
    /// <param name="username">The user name used to authenticate.</param>
    public void Username(string username)
    {
        _settings.Username = username;
    }

    /// <summary>Sets the broker password.</summary>
    /// <param name="password">The password used to authenticate.</param>
    public void Password(string password)
    {
        _settings.Password = password;
    }

    /// <summary>Enables or disables TLS for the broker connection.</summary>
    /// <param name="enabled">Whether TLS is enabled.</param>
    public void UseSsl(bool enabled = true)
    {
        _settings.UseSsl = enabled;
    }

    /// <summary>Sets a snapshot of alternate broker addresses for provider failover.</summary>
    /// <param name="hosts">The failover broker addresses.</param>
    public void FailoverHosts(params Uri[] hosts)
    {
        ArgumentNullException.ThrowIfNull(hosts);
        _settings.FailoverHosts = Array.AsReadOnly((Uri[])hosts.Clone());
    }

    /// <summary>Adds native Apache NMS connection URI options.</summary>
    /// <param name="options">The option name/value pairs; duplicate names are rejected.</param>
    public void TransportOptions(IEnumerable<KeyValuePair<string, string>> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (KeyValuePair<string, string> option in options)
        {
            if (string.IsNullOrWhiteSpace(option.Key))
                throw new ArgumentException("An ActiveMQ transport option key must not be empty or whitespace.", nameof(options));
            if (option.Value == null)
                throw new ArgumentException($"The ActiveMQ transport option '{option.Key}' must have a value.", nameof(options));
            if (!_settings.TransportOptions.TryAdd(option.Key, option.Value))
                throw new ArgumentException($"The ActiveMQ transport option '{option.Key}' was specified more than once.", nameof(options));
        }
    }

    /// <summary>Enables the Apache NMS asynchronous-send connection option.</summary>
    public void EnableAsyncSend()
    {
        _settings.TransportOptions["nms.AsyncSend"] = "true";
    }

    /// <summary>Enables the Apache NMS optimized-acknowledgement connection option.</summary>
    public void EnableOptimizeAcknowledge()
    {
        _settings.TransportOptions["jms.optimizeAcknowledge"] = "true";
    }

    /// <summary>Sets the native prefetch limit for all destination types.</summary>
    /// <param name="limit">The provider prefetch limit.</param>
    public void SetPrefetchPolicy(int limit)
    {
        _settings.TransportOptions["jms.prefetchPolicy.all"] = limit.ToString();
    }

    /// <summary>Sets the native prefetch limit for queues.</summary>
    /// <param name="limit">The queue prefetch limit.</param>
    public void SetQueuePrefetchPolicy(int limit)
    {
        _settings.TransportOptions["jms.prefetchPolicy.queuePrefetch"] = limit.ToString();
    }
}
