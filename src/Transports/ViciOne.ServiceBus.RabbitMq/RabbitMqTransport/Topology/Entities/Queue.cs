using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Describes a queue declaration in a RabbitMQ broker topology.</summary>
public interface Queue
{
    /// <summary>Gets the queue name.</summary>
    string QueueName { get; }

    /// <summary>Gets whether the queue survives broker restarts.</summary>
    bool Durable { get; }

    /// <summary>Gets whether RabbitMQ deletes the queue after its last consumer is gone.</summary>
    bool AutoDelete { get; }

    /// <summary>Gets whether the queue belongs exclusively to its declaring connection.</summary>
    bool Exclusive { get; }

    /// <summary>Gets the broker-specific queue declaration arguments.</summary>
    IDictionary<string, object?> QueueArguments { get; }
}
