using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Identifies an exchange-to-exchange binding stored in a topology builder.</summary>
public interface ExchangeBindingHandle :
    EntityHandle
{
    /// <summary>Gets the binding declaration represented by the handle.</summary>
    ExchangeToExchangeBinding Binding { get; }
}
