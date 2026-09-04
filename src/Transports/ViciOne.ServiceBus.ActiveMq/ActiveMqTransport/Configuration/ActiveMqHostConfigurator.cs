using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq host configurator implementation.
/// </summary>
public class ActiveMqHostConfigurator :
    IActiveMqHostConfigurator
{
    readonly ConfigurationHostSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
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

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public ActiveMqHostSettings Settings => _settings;

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
    /// Configures ssl for the current pipeline.
    /// </summary>
    /// <param name="enabled">The enabled value.</param>
    public void UseSsl(bool enabled = true)
    {
        _settings.UseSsl = enabled;
    }

    /// <summary>
    /// Performs the failover hosts operation.
    /// </summary>
    /// <param name="hosts">The hosts value.</param>
    public void FailoverHosts(params Uri[] hosts)
    {
        ArgumentNullException.ThrowIfNull(hosts);
        _settings.FailoverHosts = Array.AsReadOnly((Uri[])hosts.Clone());
    }

    /// <summary>
    /// Performs the transport options operation.
    /// </summary>
    /// <param name="options">The options value.</param>
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

    /// <summary>
    /// Performs the enable async send operation.
    /// </summary>
    public void EnableAsyncSend()
    {
        _settings.TransportOptions["nms.AsyncSend"] = "true";
    }

    /// <summary>
    /// Performs the enable optimize acknowledge operation.
    /// </summary>
    public void EnableOptimizeAcknowledge()
    {
        _settings.TransportOptions["jms.optimizeAcknowledge"] = "true";
    }

    /// <summary>
    /// Sets prefetch policy.
    /// </summary>
    /// <param name="limit">The limit value.</param>
    public void SetPrefetchPolicy(int limit)
    {
        _settings.TransportOptions["jms.prefetchPolicy.all"] = limit.ToString();
    }

    /// <summary>
    /// Sets queue prefetch policy.
    /// </summary>
    /// <param name="limit">The limit value.</param>
    public void SetQueuePrefetchPolicy(int limit)
    {
        _settings.TransportOptions["jms.prefetchPolicy.queuePrefetch"] = limit.ToString();
    }
}
