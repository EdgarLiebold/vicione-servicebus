using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Defines the contract for exchange binding handle.
/// </summary>
public interface ExchangeBindingHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the binding value.
    /// </summary>
    ExchangeToExchangeBinding Binding { get; }
}
