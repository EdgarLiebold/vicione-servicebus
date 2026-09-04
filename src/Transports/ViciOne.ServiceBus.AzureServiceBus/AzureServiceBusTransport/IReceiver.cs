using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for receiver.
/// </summary>
public interface IReceiver :
    IAgent,
    DeliveryMetrics
{
    /// <summary>
    /// Starts the configured component.
    /// </summary>
    void Start();
}
