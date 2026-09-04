namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq queue binding configurator implementation.
/// </summary>
public class ActiveMqQueueBindingConfigurator :
    ActiveMqQueueConfigurator,
    IActiveMqQueueBindingConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    protected ActiveMqQueueBindingConfigurator(string queueName, bool durable, bool autoDelete)
        : base(queueName, durable, autoDelete)
    {
    }

    /// <summary>
    /// Gets or sets the selector value.
    /// </summary>
    public string? Selector { get; set; }
}
