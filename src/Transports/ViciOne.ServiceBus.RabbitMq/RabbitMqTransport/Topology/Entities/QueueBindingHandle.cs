using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Identifies an exchange-to-queue binding stored in a topology builder.</summary>
public interface QueueBindingHandle :
    EntityHandle
{
    /// <summary>Gets the binding declaration represented by the handle.</summary>
    ExchangeToQueueBinding Binding { get; }
}
