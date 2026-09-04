using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Defines the contract for queue handle.
/// </summary>
public interface QueueHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the queue value.
    /// </summary>
    Queue Queue { get; }
}
