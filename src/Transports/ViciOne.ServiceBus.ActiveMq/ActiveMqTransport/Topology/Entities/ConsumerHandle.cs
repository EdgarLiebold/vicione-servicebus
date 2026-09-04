using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Defines the contract for consumer handle.
/// </summary>
public interface ConsumerHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the consumer value.
    /// </summary>
    Consumer Consumer { get; }
}
