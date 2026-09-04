using System;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq queue receive settings implementation.
/// </summary>
public class ActiveMqQueueReceiveSettings :
    ActiveMqQueueBindingConfigurator,
    ReceiveSettings
{
    readonly IActiveMqEndpointConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public ActiveMqQueueReceiveSettings(IActiveMqEndpointConfiguration configuration, string queueName, bool durable, bool autoDelete)
        : base(queueName, durable, autoDelete)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int PrefetchCount => _configuration.Transport.PrefetchCount;
    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int ConcurrentMessageLimit => _configuration.Transport.GetConcurrentMessageLimit();

    /// <summary>
    /// Gets input address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public Uri GetInputAddress(Uri hostAddress)
    {
        return GetEndpointAddress(hostAddress);
    }
}
