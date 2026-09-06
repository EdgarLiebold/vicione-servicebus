using System;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Defines queue, selector, prefetch, and concurrency settings for an ActiveMQ receive endpoint.</summary>
public class ActiveMqQueueReceiveSettings :
    ActiveMqQueueBindingConfigurator,
    ReceiveSettings
{
    readonly IActiveMqEndpointConfiguration _configuration;

    /// <summary>Creates receive settings for an ActiveMQ queue.</summary>
    /// <param name="configuration">The endpoint configuration supplying transport limits.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="durable">Whether the queue persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the queue when it is no longer used.</param>
    public ActiveMqQueueReceiveSettings(IActiveMqEndpointConfiguration configuration, string queueName, bool durable, bool autoDelete)
        : base(queueName, durable, autoDelete)
    {
        _configuration = configuration;
    }

    /// <summary>Gets the configured broker prefetch count.</summary>
    public int PrefetchCount => _configuration.Transport.PrefetchCount;
    /// <summary>Gets the effective concurrent-message limit.</summary>
    public int ConcurrentMessageLimit => _configuration.Transport.GetConcurrentMessageLimit();

    /// <summary>Builds the endpoint input address against a broker host.</summary>
    /// <param name="hostAddress">The configured broker host address.</param>
    /// <returns>The absolute queue input address.</returns>
    public Uri GetInputAddress(Uri hostAddress)
    {
        return GetEndpointAddress(hostAddress);
    }
}
