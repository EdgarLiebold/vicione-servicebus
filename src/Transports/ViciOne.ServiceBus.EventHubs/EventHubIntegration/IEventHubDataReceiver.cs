using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for event hub data receiver.
/// </summary>
public interface IEventHubDataReceiver :
    IAgent,
    DeliveryMetrics
{
}
