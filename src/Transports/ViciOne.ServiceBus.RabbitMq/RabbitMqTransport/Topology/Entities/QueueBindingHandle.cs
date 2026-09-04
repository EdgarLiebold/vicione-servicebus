using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Defines the contract for queue binding handle.
/// </summary>
public interface QueueBindingHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the binding value.
    /// </summary>
    ExchangeToQueueBinding Binding { get; }
}
