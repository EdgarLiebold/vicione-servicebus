using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Identifies an exchange stored in a topology builder.</summary>
public interface ExchangeHandle :
    EntityHandle
{
    /// <summary>Gets the exchange declaration represented by the handle.</summary>
    Exchange Exchange { get; }
}
