using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq queue configurator implementation.
/// </summary>
public class ActiveMqQueueConfigurator :
    EntityConfigurator,
    IActiveMqQueueConfigurator,
    Queue
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    protected ActiveMqQueueConfigurator(string queueName, bool durable = true, bool autoDelete = false)
        : base(queueName, durable, autoDelete)
    {
    }

    /// <summary>
    /// Gets the address type value.
    /// </summary>
    protected override ActiveMqEndpointAddress.AddressType AddressType => ActiveMqEndpointAddress.AddressType.Queue;
}
