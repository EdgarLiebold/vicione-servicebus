using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Identifies a queue stored in a topology builder.</summary>
public interface QueueHandle :
    EntityHandle
{
    /// <summary>Gets the queue declaration represented by the handle.</summary>
    Queue Queue { get; }
}
