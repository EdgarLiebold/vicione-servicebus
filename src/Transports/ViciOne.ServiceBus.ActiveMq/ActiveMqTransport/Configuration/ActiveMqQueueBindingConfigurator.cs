namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Configures an ActiveMQ queue binding and its optional message selector.</summary>
public class ActiveMqQueueBindingConfigurator :
    ActiveMqQueueConfigurator,
    IActiveMqQueueBindingConfigurator
{
    /// <summary>Creates a queue-binding configurator.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="durable">Whether the queue persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the queue when it is no longer used.</param>
    protected ActiveMqQueueBindingConfigurator(string queueName, bool durable, bool autoDelete)
        : base(queueName, durable, autoDelete)
    {
    }

    /// <summary>Gets or sets the Apache NMS message selector applied by the binding.</summary>
    public string? Selector { get; set; }
}
