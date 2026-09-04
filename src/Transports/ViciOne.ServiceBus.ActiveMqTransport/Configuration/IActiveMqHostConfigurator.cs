using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus;

public interface IActiveMqHostConfigurator
{
    /// <summary>
    /// Sets the username for the connection to ActiveMQ
    /// </summary>
    /// <param name="username"></param>
    void Username(string username);

    /// <summary>
    /// Sets the password for the connection to ActiveMQ
    /// </summary>
    /// <param name="password"></param>
    void Password(string password);

    void UseSsl(bool enabled = true);

    /// <summary>
    /// Sets a list of hosts to enable the failover transport
    /// </summary>
    /// <param name="hosts">Absolute broker endpoints matching the configured protocol and TLS mode.</param>
    void FailoverHosts(params Uri[] hosts);

    /// <summary>
    /// Sets options on the underlying NMS transport
    /// </summary>
    /// <param name="options"></param>
    void TransportOptions(IEnumerable<KeyValuePair<string, string>> options);

    /// <summary>
    /// </summary>
    void EnableOptimizeAcknowledge();

    void SetPrefetchPolicy(int limit);

    void SetQueuePrefetchPolicy(int limit);

    /// <summary>
    /// Enables nms.AsyncSend. It is disabled by default because broker failures can otherwise
    /// result in message loss. It can also be enabled using <see cref="TransportOptions"/>.
    /// </summary>
    void EnableAsyncSend();
}
