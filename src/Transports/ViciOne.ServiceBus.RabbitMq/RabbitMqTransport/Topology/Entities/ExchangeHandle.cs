using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Defines the contract for exchange handle.
/// </summary>
public interface ExchangeHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the exchange value.
    /// </summary>
    Exchange Exchange { get; }
}
