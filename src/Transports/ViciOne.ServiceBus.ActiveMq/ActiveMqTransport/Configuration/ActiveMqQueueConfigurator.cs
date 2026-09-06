using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Configures an ActiveMQ queue entity.</summary>
public class ActiveMqQueueConfigurator :
    EntityConfigurator,
    IActiveMqQueueConfigurator,
    Queue
{
    /// <summary>Creates a queue configurator.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="durable">Whether the queue persists across broker restarts.</param>
    /// <param name="autoDelete">Whether the broker removes the queue when it is no longer used.</param>
    protected ActiveMqQueueConfigurator(string queueName, bool durable = true, bool autoDelete = false)
        : base(queueName, durable, autoDelete)
    {
    }

    /// <summary>Gets the queue address type.</summary>
    protected override ActiveMqEndpointAddress.AddressType AddressType => ActiveMqEndpointAddress.AddressType.Queue;
}
