using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures ActiveMQ credentials, TLS, failover, and native provider options.</summary>
public interface IActiveMqHostConfigurator
{
    /// <summary>Sets the broker user name.</summary>
    /// <param name="username">The user name used to authenticate.</param>
    void Username(string username);

    /// <summary>Sets the broker password.</summary>
    /// <param name="password">The password used to authenticate.</param>
    void Password(string password);

    /// <summary>Enables or disables TLS for the broker connection.</summary>
    /// <param name="enabled">Whether TLS is enabled.</param>
    void UseSsl(bool enabled = true);

    /// <summary>Sets a list of hosts to enable the failover transport.</summary>
    /// <param name="hosts">Absolute broker endpoints matching the configured protocol and TLS mode.</param>
    void FailoverHosts(params Uri[] hosts);

    /// <summary>Adds options to the underlying Apache NMS transport URI.</summary>
    /// <param name="options">The option name/value pairs; duplicate names are rejected.</param>
    void TransportOptions(IEnumerable<KeyValuePair<string, string>> options);

    /// <summary>Enables the native optimized-acknowledgement option.</summary>
    void EnableOptimizeAcknowledge();

    /// <summary>Sets the native prefetch limit for all destination types.</summary>
    /// <param name="limit">The provider prefetch limit.</param>
    void SetPrefetchPolicy(int limit);

    /// <summary>Sets the native prefetch limit for queues.</summary>
    /// <param name="limit">The queue prefetch limit.</param>
    void SetQueuePrefetchPolicy(int limit);

    /// <summary>
    /// Enables <c>nms.AsyncSend</c>. It is disabled by default because broker failures can otherwise
    /// result in message loss. It can also be enabled using <see cref="TransportOptions"/>.
    /// </summary>
    void EnableAsyncSend();
}
