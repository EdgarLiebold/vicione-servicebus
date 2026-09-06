using System;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines the operations required by queue.</summary>
public interface Queue
{
    /// <summary>Gets the queue name.</summary>
    string QueueName { get; }

    /// <summary>Idle time before queue should be deleted (consumer-idle, not producer).</summary>
    TimeSpan? AutoDeleteOnIdle { get; }

    /// <summary>Specify the maximum delivery count for messages in the queue.</summary>
    int? MaxDeliveryCount { get; }
}
